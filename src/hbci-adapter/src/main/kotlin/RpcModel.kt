import kotlinx.serialization.Serializable

@Serializable
class RpcSyncRequest(
    val AccountId: String,
    val HbciVersion: String,
    val Blz: String,
    val User: String,
    val CustomerId: String,
    val Pin: String,
    val StartDate: String? = null
)

@Serializable
class RpcSyncResponse(
    val Accounts: Array<RpcSyncAccountResponse>
)

@Serializable
class RpcDone()

@Serializable
class RpcLogEntry(
    val Severity: Int,
    val Message: String
)

val SEVERITY_ERROR = 3;
val SEVERITY_WARNING= 2;
val SEVERITY_INFO = 1;
val SEVERITY_TRACE = 0;

@Serializable
class RpcSecurityMechanismRequest(val Entries: Array<RpcSecurityMechanismRequestEntry>)

@Serializable
class RpcSecurityMechanismRequestEntry(val Code: String, val Name: String)

@Serializable
class RpcSecurityMechanismResponse(val Code: String);

@Serializable
class RpcTanResponse(val Tan: String)

@Serializable
class RpcTanRequest(val Message: String)

@Serializable
class RpcSyncAccountResponse(
    val Name: String,
    val Name2: String?,
    val Country: String,
    val Currency: String,
    val Bic: String,
    val Iban: String,
    val Blz: String,
    val Number: String,
    val SubNumber: String?,
    val CustomerId: String,
    val AccountType: String,
    val Type: String,
    val Balance: Long,
    val Transactions: Array<RpcSyncAccountTransactionResponse>
)

@Serializable
class RpcSyncAccountTransactionResponse(
    val Id: String?,
    val Date: String,
    val Usage: String,
    val Code: String,
    val Amount: Long,
    val OriginalAmount: Long?,
    val ChargeAmount: Long?,
    val Balance: Long,
    val IsStorno: Boolean,
    val CustomerReference: String,
    val InstituteReference: String,
    val Additional: String?,
    val Text: String,
    val Primanota: String,
    val AddKey: String?,
    val IsSepa: Boolean,
    val IsCamt: Boolean,
    val EndToEndId: String?,
    val PurposeCode: String?,
    val ManadateId: String?
)
