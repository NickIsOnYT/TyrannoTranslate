py -3.14 -m pip install -r requirements.txt
dotnet publish TyrannoTranslate/TyrannoTranslate.csproj -c Release -r win-x64
dotnet run --project TyrannoTranslate
