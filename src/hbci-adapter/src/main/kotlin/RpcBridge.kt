
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json
import java.nio.ByteBuffer
import java.nio.ByteOrder
import java.nio.charset.StandardCharsets
import kotlin.text.toByteArray

class RpcBridge {
    fun connect() {
        val version = readInt();
        if (version != 1) {
            throw Exception("Unsupported RPC protocol version received. Version $version was requested by the client but only '1' is supported.");
        }
    }

    inline fun <reified T>send(obj: T) {
        writeString(T::class.simpleName.toString())
        writeString(Json.encodeToString(obj))
    }

    inline fun<reified T> read(): T {
        val messageType = readString();
        if (messageType != T::class.simpleName) {
            throw Exception("Message of type '${T::class.simpleName}' expected but message of type '${messageType}' received.")
        }
        val json = readString()
        return Json.decodeFromString<T>(json)
    }

    fun readString(): String {
        val len = readInt()
        val buffer = System.`in`.readNBytes(len)
        return String(buffer, StandardCharsets.UTF_8)
    }

    fun writeString(value: String) {
        val bytes = value.toByteArray(StandardCharsets.UTF_8)
        writeInt(bytes.size);
        System.out.writeBytes(bytes)
        System.out.flush()
    }

    private fun readInt(): Int {
        return ByteBuffer
            .wrap(System.`in`.readNBytes(4))
            .order(ByteOrder.LITTLE_ENDIAN)
            .getInt()
    }

    private fun writeInt(value: Int) {
        val buffer = ByteBuffer
            .wrap(ByteArray(4))
            .order(ByteOrder.LITTLE_ENDIAN)

        buffer.putInt(value)
        System.out.writeBytes(buffer.array())
        System.out.flush()
    }
}