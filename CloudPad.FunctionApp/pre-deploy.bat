@echo off

rd /s /q bin\Release\net461

pushd ..

node build.js

popd

rd /s /q bin\Release\net461\bin\de
rd /s /q bin\Release\net461\bin\es
rd /s /q bin\Release\net461\bin\fr
rd /s /q bin\Release\net461\bin\it
rd /s /q bin\Release\net461\bin\ja
rd /s /q bin\Release\net461\bin\ko
rd /s /q bin\Release\net461\bin\ru
rd /s /q bin\Release\net461\bin\zh-Hans
rd /s /q bin\Release\net461\bin\zh-Hant

rd /s /q bin\Release\net461\bin\runtimes\osx
rd /s /q bin\Release\net461\bin\runtimes\unix

del bin\Release\net461\local.settings.json
