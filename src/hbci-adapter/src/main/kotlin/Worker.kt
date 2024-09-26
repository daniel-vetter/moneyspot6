import org.kapott.hbci.GV.HBCIJob
import org.kapott.hbci.GV_Result.GVRKUms
import org.kapott.hbci.GV_Result.GVRSaldoReq
import org.kapott.hbci.manager.HBCIHandler
import org.kapott.hbci.manager.HBCIUtils
import org.kapott.hbci.passport.HBCIPassport
import org.kapott.hbci.passport.HBCIPassportPinTan
import org.kapott.hbci.status.HBCIExecStatus
import org.kapott.hbci.structures.Konto
import java.io.File
import java.text.SimpleDateFormat
import java.util.*


class Worker(val rpc: RpcBridge) {
    fun run() {
        val request = rpc.read<RpcSyncRequest>()

        initHbciLibrary(rpc, request.BankCode, request.UserId, request.CustomerId, request.Pin)
        var handle: HBCIHandler? = null
        var passport: HBCIPassport? = null
        try {

            log(SEVERITY_INFO, "Connecting...")
            passport = createPassport(request.AccountId, request.BankCode)
            handle = HBCIHandler(request.HbciVersion, passport)

            log(SEVERITY_INFO, "Retrieving list of accounts...")
            val accounts = passport.accounts ?: emptyArray();

            log(SEVERITY_INFO, "Requesting transactions...")
            val startDate = if (request.StartDate == null) null else SimpleDateFormat("yyyy-MM-dd").parse(request.StartDate)
            val jobs = createQueryJobs(accounts, handle, startDate)
            val status = handle.execute()

            log(SEVERITY_INFO, "Processing response...")
            if (!validateHbciResult(status, jobs))
                return
            rpc.send(mapResultToRpcModel(jobs))

            log(SEVERITY_INFO, "Done")

        }
        catch(e: Exception) {
            rpc.send(RpcException(e.toString()))
        }
        finally {
            handle?.close()
            passport?.close()
        }
    }

    private fun initHbciLibrary(rpc: RpcBridge, bankCode: String, user: String, customerId: String, pin: String) {
        HBCIUtils.init(Properties(), CallbackHandler(rpc, bankCode, user, customerId, pin))
        HBCIUtils.setParam("log.loglevel.default", "100")
        HBCIUtils.setParam("client.passport.default", "PinTan") // Legt als Verfahren PIN/TAN fest.
        HBCIUtils.setParam("client.passport.PinTan.init", "1") // Stellt sicher, dass der Passport initialisiert wird
    }

    private fun createPassport(accountId: String, bankCode: String): HBCIPassport {
        val passportFile: File = File("${accountId}.dat")
        val passport: HBCIPassport = HBCIPassportPinTan(passportFile)
        passport.country = "DE"
        passport.host = HBCIUtils.getBankInfo(bankCode).pinTanAddress
        passport.port = 443
        passport.filterType = "Base64"
        return passport
    }

    private fun createQueryJobs(accounts: Array<Konto>, handle: HBCIHandler, startDate: Date?): ArrayList<RunningJob> {
        val jobs: ArrayList<RunningJob> = ArrayList<RunningJob>()
        for (account in accounts) {
            val runningJob = RunningJob(
                account = account,
                balanceJob = handle.newJob("SaldoReq"),
                transactionsJob = handle.newJob("KUmsAll")
            )

            runningJob.balanceJob.setParam("my", account)
            runningJob.balanceJob.addToQueue()

            runningJob.transactionsJob.setParam("my", account)
            if (startDate != null) {
                runningJob.transactionsJob.setParam("startdate", startDate)
            }
            runningJob.transactionsJob.addToQueue()

            jobs.add(runningJob)
        }
        return jobs
    }

    private fun validateHbciResult(status: HBCIExecStatus, jobs: ArrayList<RunningJob>): Boolean {
        if (!status.isOK) {
            log(SEVERITY_ERROR, status.toString())
            return false
        }

        var isValid = true
        for (job in jobs) {
            if (!job.balanceJob.jobResult.isOK) {
                log(SEVERITY_ERROR, job.balanceJob.toString())
                isValid = false
            }
            if (!job.transactionsJob.jobResult.isOK) {
                log(SEVERITY_ERROR, job.transactionsJob.toString())
                isValid = false
            }
        }

        return isValid
    }

    private fun mapResultToRpcModel(jobs: ArrayList<RunningJob>): RpcSyncResponse {
        val dateFormat = SimpleDateFormat("yyyy-MM-dd")
        return RpcSyncResponse(
            jobs.map {
                RpcSyncAccountResponse(
                    Name = it.account.name,
                    Name2 = it.account.name2,
                    Country = it.account.country,
                    Bic = it.account.bic,
                    Iban = it.account.iban,
                    Currency = it.account.curr,
                    CustomerId = it.account.customerid,
                    AccountType = it.account.acctype,
                    BankCode = it.account.blz,
                    Number = it.account.number,
                    SubNumber = it.account.subnumber,
                    Type = it.account.type,
                    Balance = (it.balanceJob.jobResult as GVRSaldoReq).entries[0].ready.value.longValue,
                    Transactions = (it.transactionsJob.jobResult as GVRKUms).flatData.map { l ->
                        RpcSyncAccountTransactionResponse(
                            Id = l.id,
                            Date = dateFormat.format(l.bdate),
                            AccountName = l.other?.name,
                            AccountName2 = l.other?.name2,
                            AccountCountry = l.other?.country,
                            AccountBankCode = l.other?.blz,
                            AccountNumber = l.other?.number,
                            AccountBic = l.other?.bic,
                            AccountIban = l.other?.iban,
                            Usage = l.usage,
                            Code = l.gvcode,
                            Amount = l.value.longValue,
                            OriginalAmount = l.orig_value?.longValue,
                            ChargeAmount = l.charge_value?.longValue,
                            Balance = l.saldo.value.longValue,
                            IsStorno = l.isStorno,
                            CustomerReference = l.customerref,
                            InstituteReference = l.instref,
                            Additional = l.additional,
                            Text = l.text,
                            Primanota = l.primanota,
                            AddKey = l.addkey,
                            IsSepa = l.isSepa,
                            IsCamt = l.isCamt,
                            EndToEndId = l.endToEndId,
                            PurposeCode = l.purposecode,
                            MandateId = l.mandateId
                        )
                    }.toTypedArray()
                )
            }.toTypedArray()
        )
    }

    private fun log(severity: Int, msg: String) {
        rpc.send(RpcLogEntry(severity, msg))
    }
    private class RunningJob(
        val account: Konto,
        val balanceJob: HBCIJob,
        val transactionsJob: HBCIJob
    )
}