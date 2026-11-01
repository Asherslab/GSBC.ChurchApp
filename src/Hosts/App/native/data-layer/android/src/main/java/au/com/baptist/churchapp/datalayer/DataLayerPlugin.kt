package au.com.baptist.churchapp.datalayer

import com.getcapacitor.Plugin
import com.getcapacitor.PluginCall
import com.getcapacitor.PluginMethod
import com.getcapacitor.annotation.CapacitorPlugin

@CapacitorPlugin(name = "DataLayer")
class DataLayerPlugin : Plugin() {
    @PluginMethod fun unary(call: PluginCall) = unavailable(call)
    @PluginMethod fun readPage(call: PluginCall) = unavailable(call)
    @PluginMethod fun startSync(call: PluginCall) = unavailable(call)
    @PluginMethod fun stopSync(call: PluginCall) = unavailable(call)
    @PluginMethod fun enqueue(call: PluginCall) = unavailable(call)

    private fun unavailable(call: PluginCall) = call.unavailable("Android is stubbed until iOS ships.")
}
