The wasm compilation in this project is done via [Native AOT LLVM](https://github.com/dotnet/runtimelab/tree/feature/NativeAOT-LLVM).

First read [using NativeAOT](https://github.com/dotnet/runtimelab/tree/feature/NativeAOT-LLVM/docs/using-nativeaot)

In my experience is best to build on Linux/WSL, i have specifically used Ubuntu 24.04.4 LTS but other distros should work as well. 

You will need to install the [Emscripten SDK](https://emscripten.org/docs/tools_reference/emsdk.html) then install the latest version and activate it:
```bash
# Fetch the latest registry of available tools.
./emsdk update

# Download and install the latest SDK tools.
./emsdk install latest

# Set up the compiler configuration to point to the "latest" SDK.
./emsdk activate latest
```


then you can build with the publish-all-projects-wasm.ps1 script, which will build all the projects in the solution with the wasm target:
```bash 
pwsh ./publish-all-projects-wasm.ps1
```

or to just build a specific project, you can use the following command:
```bash
dotnet publish -r browser-wasm -c Release
```