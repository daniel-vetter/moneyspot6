
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json
import kotlinx.serialization.serializer
import java.nio.ByteBuffer
import java.nio.ByteOrder
import java.nio.charset.StandardCharsets
import kotlin.text.toByteArray
import kotlin.reflect.KClass

abstract class RpcBridge {
    abstract fun connect()
    abstract fun <T : Any>sendWithClass(obj: T, clazz: KClass<T>)
    abstract fun <T : Any>readWithClass(clazz: KClass<T>): T

    inline fun <reified T : Any>send(obj: T) {
        sendWithClass(obj, T::class)
    }

    inline fun<reified T : Any> read(): T {
        return readWithClass(T::class)
    }
}

class FakeRpcBridge(private val preparedRequest: RpcSyncRequest) : RpcBridge() {
    override fun connect() {}

    @Suppress("UNCHECKED_CAST")
    override fun <T : Any> readWithClass(clazz: KClass<T>): T {
        if (clazz != RpcSyncRequest::class) {
            throw Exception("FakeRpcBridge only supports reading RpcSyncRequest")
        }
        return preparedRequest as T
    }

    override fun <T : Any> sendWithClass(obj: T, clazz: KClass<T>) {
        if (obj is RpcLogEntry) {
            println(obj.Message)
        }
        if (obj is RpcException) {
            println(obj.Message)
        }
    }
}

class StdioRpcBridge : RpcBridge() {
    override fun connect() {
        val version = readInt()
        if (version != 1) {
            throw Exception("Unsupported RPC protocol version received. Version $version was requested by the client but only '1' is supported.")
        }
    }

    override fun <T : Any>sendWithClass(obj: T, clazz: KClass<T>) {
        writeString(clazz.simpleName.toString())
        val serializer = serializer(clazz.java)
        writeString(Json.encodeToString(serializer, obj))
    }

    @Suppress("UNCHECKED_CAST")
    override fun<T : Any> readWithClass(clazz: KClass<T>): T {
        val messageType = readString()
        if (messageType != clazz.simpleName) {
            throw Exception("Message of type '${clazz.simpleName}' expected but message of type '$messageType' received.")
        }
        val json = readString()
        val serializer = serializer(clazz.java)
        return Json.decodeFromString(serializer, json) as T
    }

    fun readString(): String {
        val len = readInt()
        val buffer = System.`in`.readNBytes(len)
        return String(buffer, StandardCharsets.UTF_8)
    }

    fun writeString(value: String) {
        val bytes = value.toByteArray(StandardCharsets.UTF_8)
        writeInt(bytes.size)
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