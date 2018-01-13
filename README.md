Purple
===============

Bitcoin Implementation in C#
----------------------------

Purple is an implementation of the Bitcoin protocol in C# on the [.NET Core](https://dotnet.github.io/) platform built on the Stratis platform.

Purple is a hybrid PoW/ PoS cryptocurrency.
  
[.NET Core](https://dotnet.github.io/) is an open source cross platform framework and enables the development of applications and services on Windows, macOS and Linux.  

The design
----------

**A Modular Approach**

A Blockchain is made of many components, from a FullNode that validates blocks to a Simple Wallet that track addresses.
The end goal is to develop a set of [Nuget](https://en.wikipedia.org/wiki/NuGet) packages from which an implementer can cherry pick what he needs.

* **NBitcoin**
* **Purple.Bitcoin.Core**  - The bare minimum to run a pruned node.
* **Purple.Bitcoin.Store** - Store and relay blocks to peers.
* **Purple.Bitcoin.MemoryPool** - Track pending transaction.
* **Purple.Bitcoin.Wallet** - Send and Receive coins
* **Purple.Bitcoin.Miner** - POS or POW
* **Purple.Bitcoin.Explorer**


Create a Blockchain in a .NET Core style programming
```
  var node = new FullNodeBuilder()
   .UseNodeSettings(nodeSettings)
   .UseConsensus()
   .UseBlockStore()
   .UseMempool()
   .AddMining()
   .AddRPC()
   .Build();

  node.Run();
```

What's Next
----------

We plan to add many more features on top of the Purple Bitcoin blockchain:

Running a FullNode
------------------

Our full node is currently in alpha.  

```
git clone https://github.com/glasgowdev/Purple.git  
cd Purple\src

dotnet restore
dotnet build

```

To run on the Bitcoin network: ``` Purple.BitcoinD\dotnet run ```  
To run on the Purple network: ``` Purple.StratisD\dotnet run ```  

See more details [here](https://github.com/glasgowdev/Purple/blob/master/Documentation/getting-started.md)

Testing
-------
* [Testing Guidelines](Documentation/testing-guidelines.md)

CI build
-----------

We use [AppVeyor](https://www.appveyor.com/) for our CI build and to create nuget packages.
Every time someone pushes to the master branch or create a pull request on it, a build is triggered and new nuget packages are created.

To skip a build, for example if you've made very minor changes, include the text **[skip ci]** or **[ci skip]** in your commits' comment (with the squared brackets).

If you want get the :sparkles: latest :sparkles: (and unstable :bomb:) version of the nuget packages here: 
* [Purple.Bitcoin](https://ci.appveyor.com/api/projects/stratis/stratisbitcoinfullnode/artifacts/nuget/Purple.Bitcoin.1.0.7-alpha.nupkg?job=Configuration%3A%20Release)

License
-----------

MIT License

Purple Bitcoin is based on the [Stratis] (https://github.com/stratisproject/StratisBitcoinFullNode) and [NBitcoin](https://github.com/MetacoSA/NBitcoin) project.  

Proof of Stake support on the Purple token the node is using [NStratis] (https://github.com/stratisproject/NStratis) which is a POS implementation of NBitcoin.  
