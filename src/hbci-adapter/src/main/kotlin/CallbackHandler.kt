import org.kapott.hbci.callback.AbstractHBCICallback
import org.kapott.hbci.exceptions.HBCI_Exception
import org.kapott.hbci.passport.HBCIPassport
import java.io.IOException
import java.net.SecureCacheResponse
import java.util.*

class CallbackHandler(
    private val rpc: RpcBridge,
    private val blz: String,
    private val user: String,
    private val customerId: String,
    private val pin: String
) :
    AbstractHBCICallback() {
    /**
     * @see org.kapott.hbci.callback.HBCICallback.log
     */
    override fun log(msg: String, level: Int, date: Date, trace: StackTraceElement) {
        rpc.send(RpcLogEntry(SEVERITY_TRACE, msg))
    }

    /**
     * @see org.kapott.hbci.callback.HBCICallback.callback
     */
    override fun callback(passport: HBCIPassport, reason: Int, msg: String, datatype: Int, retData: StringBuffer) {
        // Diese Funktion ist wichtig. Ueber die fragt HBCI4Java die benoetigten Daten von uns ab.
        when (reason) {
            NEED_PASSPHRASE_LOAD, NEED_PASSPHRASE_SAVE -> retData.replace(0, retData.length, this.pin)
            NEED_PT_PIN -> retData.replace(0, retData.length, this.pin)
            NEED_BLZ -> retData.replace(0, retData.length, this.blz)
            NEED_USERID -> retData.replace(0, retData.length, this.user)
            NEED_CUSTOMERID -> retData.replace(0, retData.length, this.customerId)
            NEED_PT_PHOTOTAN ->                 // Die Klasse "MatrixCode" kann zum Parsen der Daten verwendet werden
                try {
                    // MatrixCode code = new MatrixCode(retData.toString());

                    // Liefert den Mime-Type der grafik (i.d.R. "image/png").
                    // String type = code.getMimetype();

                    // Der Stream enthaelt jetzt die Binaer-Daten des Bildes
                    // InputStream stream = new ByteArrayInputStream(code.getImage());

                    // .... Hier Dialog mit der Grafik anzeigen und User-Eingabe der TAN
                    // Die Variable "msg" aus der Methoden-Signatur enthaelt uebrigens
                    // den bankspezifischen Text mit den Instruktionen fuer den User.
                    // Der Text aus "msg" sollte daher im Dialog dem User angezeigt
                    // werden.

                    val tan: String? = null
                    retData.replace(0, retData.length, tan)
                } catch (e: Exception) {
                    throw HBCI_Exception(e)
                }

            NEED_PT_QRTAN ->                 // Die Klasse "QRCode" kann zum Parsen der Daten verwendet werden
                try {
                    // QRCode code = new QRCode(retData.toString(),msg);

                    // Der Stream enthaelt jetzt die Binaer-Daten des Bildes
                    // InputStream stream = new ByteArrayInputStream(code.getImage());

                    // .... Hier Dialog mit der Grafik anzeigen und User-Eingabe der TAN
                    // Die Variable "msg" aus der Methoden-Signatur enthaelt uebrigens
                    // den bankspezifischen Text mit den Instruktionen fuer den User.
                    // Der Text aus "msg" sollte daher im Dialog dem User angezeigt
                    // werden. Da Sparkassen den eigentlichen Bild u.U. auch in msg verpacken,
                    // sollte zur Anzeige nicht der originale Text verwendet werden sondern
                    // der von QRCode - dort ist dann die ggf. enthaltene Base64-codierte QR-Grafik entfernt
                    // msg = code.getMessage();

                    val tan: String? = null
                    retData.replace(0, retData.length, tan)
                } catch (e: Exception) {
                    throw HBCI_Exception(e)
                }

            NEED_PT_SECMECH -> {
                // Als Parameter werden die verfuegbaren TAN-Verfahren uebergeben.
                // Der Aufbau des String ist wie folgt:
                // <code1>:<name1>|<code2>:<name2>|...
                // Bsp:
                // 911:smsTAN|920:chipTAN optisch|955:photoTAN
                // String options = retData.toString();

                // Der Callback muss den Code des zu verwendenden TAN-Verfahrens
                // zurueckliefern
                // In "code" muss der 3-stellige Code des vom User gemaess obigen
                // Optionen ausgewaehlte Verfahren eingetragen werden

                val entries = retData.toString().split('|').map {
                    val parts = it.split(':')
                    RpcSecurityMechanismRequestEntry(parts[0], parts[1])
                }
                rpc.send(RpcSecurityMechanismRequest(entries.toTypedArray()))
                val response = rpc.read<RpcSecurityMechanismResponse>()
                retData.replace(0, retData.length, response.Code)
            }

            NEED_PT_TAN -> {
                // Wenn per "retData" Daten uebergeben wurden, dann enthalten diese
                // den fuer chipTAN optisch zu verwendenden Flickercode.
                // Falls nicht, ist es eine TAN-Abfrage, fuer die keine weiteren
                // Parameter benoetigt werden (z.Bsp. beim smsTAN-Verfahren)

                // Die Variable "msg" aus der Methoden-Signatur enthaelt uebrigens
                // den bankspezifischen Text mit den Instruktionen fuer den User.
                // Der Text aus "msg" sollte daher im Dialog dem User angezeigt
                // werden.
                val flicker = retData.toString()
                if (flicker != null && flicker.length > 0) {
                    throw RuntimeException("Flicker stuff")
                } else {
                    rpc.send(RpcTanRequest(msg))
                    val response = rpc.read<RpcTanResponse>()
                    retData.replace(0, retData.length, response.Tan)
                }
            }

            NEED_PT_TANMEDIA -> {
                // Als Parameter werden die verfuegbaren TAN-Medien uebergeben.
                // Der Aufbau des String ist wie folgt:
                // <name1>|<name2>|...
                // Bsp:
                // Privathandy|Firmenhandy
                // String options = retData.toString();

                // Der Callback muss den vom User ausgewaehlten Aliasnamen
                // zurueckliefern. Falls "options" kein "|" enthaelt, ist davon
                // auszugehen, dass nur eine moegliche Option existiert. In dem
                // Fall ist keine Auswahl noetig und "retData" kann unveraendert
                // bleiben
                val alias: String? = null
                retData.replace(0, retData.length, alias)
            }

            HAVE_ERROR -> {
                //TODO
            }

            else -> {}
        }
    }

    override fun status(passport: HBCIPassport, statusTag: Int, o: Array<Any>?) {
        // So aehnlich wie log(String,int,Date,StackTraceElement) jedoch fuer Status-Meldungen.
    }
}