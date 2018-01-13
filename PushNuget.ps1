
rm "src\Purple.Bitcoin\bin\release\" -Recurse -Force
dotnet pack src\Purple.Bitcoin --configuration Release  
dotnet nuget push "src\Purple.Bitcoin\bin\Release\*.nupkg" --source "https://api.nuget.org/v3/index.json"

rm "src\Purple.Bitcoin.Features.Api\bin\release\" -Recurse -Force
dotnet pack src\Purple.Bitcoin.Features.Api --configuration Release  
dotnet nuget push "src\Purple.Bitcoin.Features.Api\bin\Release\*.nupkg" --source "https://api.nuget.org/v3/index.json"

rm "src\Purple.Bitcoin.Features.BlockStore\bin\release\" -Recurse -Force
dotnet pack src\Purple.Bitcoin.Features.BlockStore --configuration Release  
dotnet nuget push "src\Purple.Bitcoin.Features.BlockStore\bin\Release\*.nupkg" --source "https://api.nuget.org/v3/index.json"

rm "src\Purple.Bitcoin.Features.Consensus\bin\release\" -Recurse -Force
dotnet pack src\Purple.Bitcoin.Features.Consensus --configuration Release  
dotnet nuget push "src\Purple.Bitcoin.Features.Consensus\bin\Release\*.nupkg" --source "https://api.nuget.org/v3/index.json"

rm "src\Purple.Bitcoin.Features.LightWallet\bin\release\" -Recurse -Force
dotnet pack src\Purple.Bitcoin.Features.LightWallet --configuration Release  
dotnet nuget push "src\Purple.Bitcoin.Features.LightWallet\bin\Release\*.nupkg" --source "https://api.nuget.org/v3/index.json"

rm "src\Purple.Bitcoin.Features.MemoryPool\bin\release\" -Recurse -Force
dotnet pack src\Purple.Bitcoin.Features.MemoryPool --configuration Release 
dotnet nuget push "src\Purple.Bitcoin.Features.MemoryPool\bin\Release\*.nupkg" --source "https://api.nuget.org/v3/index.json"

rm "src\Purple.Bitcoin.Features.Miner\bin\release\" -Recurse -Force
dotnet pack src\Purple.Bitcoin.Features.Miner --configuration Release  
dotnet nuget push "src\Purple.Bitcoin.Features.Miner\bin\Release\*.nupkg" --source "https://api.nuget.org/v3/index.json"

rm "src\Purple.Bitcoin.Features.Notifications\bin\release\" -Recurse -Force
dotnet pack src\Purple.Bitcoin.Features.Notifications --configuration Release  
dotnet nuget push "src\Purple.Bitcoin.Features.Notifications\bin\Release\*.nupkg" --source "https://api.nuget.org/v3/index.json"

rm "src\Purple.Bitcoin.Features.RPC\bin\release\" -Recurse -Force
dotnet pack src\Purple.Bitcoin.Features.RPC --configuration Release  
dotnet nuget push "src\Purple.Bitcoin.Features.RPC\bin\Release\*.nupkg" --source "https://api.nuget.org/v3/index.json"

rm "src\Purple.Bitcoin.Features.Wallet\bin\release\" -Recurse -Force
dotnet pack src\Purple.Bitcoin.Features.Wallet --configuration Release  
dotnet nuget push "src\Purple.Bitcoin.Features.Wallet\bin\Release\*.nupkg" --source "https://api.nuget.org/v3/index.json"

rm "src\Purple.Bitcoin.Features.WatchOnlyWallet\bin\release\" -Recurse -Force
dotnet pack src\Purple.Bitcoin.Features.WatchOnlyWallet --configuration Release  
dotnet nuget push "src\Purple.Bitcoin.Features.WatchOnlyWallet\bin\Release\*.nupkg" --source "https://api.nuget.org/v3/index.json"
