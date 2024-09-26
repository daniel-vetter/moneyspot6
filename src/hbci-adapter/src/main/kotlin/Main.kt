//TODO: StartDate als Parameter
//TODO: Passport file location in user directory

fun main() {
    val rpc = RpcBridge()
    rpc.connect()

    try {
        Worker(rpc).run();
        rpc.send(RpcDone())
    } catch (e: Exception) {
        rpc.send(RpcException("HBCI Adapter crashed: $e"))
    }
}