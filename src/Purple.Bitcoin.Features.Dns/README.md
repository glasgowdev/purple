## Purple DNS Crawler 
The Purple DNS Crawler provides a list of Purple full nodes that have recently been active via a custom DNS server.

### Prerequisites

To install and run the DNS Server, you need
* [.NET Core 2.0](https://www.microsoft.com/net/download/core)
* [Git](https://git-scm.com/)

## Build instructions

### Get the repository and its dependencies

```
git clone https://github.com/glasgowdev/purple.git  
cd purple
git submodule update --init --recursive
```

### Build and run the code
With this node, you can run the DNS Server in isolation or as a Purple node with DNS functionality:

1. To run a <b>Purple</b> node <b>only</b> on <b>MainNet</b>, do
```
cd Purple.PurpleDnsD
dotnet run -dnslistenport=5399 -dnshostname=dns.Purpleplatform.com -dnsnameserver=ns1.dns.Purpleplatform.com -dnsmailbox=admin@Purpleplatform.com
```  

2. To run a <b>Purple</b> node and <b>full node</b> on <b>MainNet</b>, do
```
cd Purple.PurpleDnsD
dotnet run -dnsfullnode -dnslistenport=5399 -dnshostname=dns.Purpleplatform.com -dnsnameserver=ns1.dns.Purpleplatform.com -dnsmailbox=admin@Purpleplatform.com
```  

3. To run a <b>Purple</b> node <b>only</b> on <b>TestNet</b>, do
```
cd Purple.PurpleDnsD
dotnet run -testnet -dnslistenport=5399 -dnshostname=dns.Purpleplatform.com -dnsnameserver=ns1.dns.Purpleplatform.com -dnsmailbox=admin@Purpleplatform.com
```  

4. To run a <b>Purple</b> node and <b>full node</b> on <b>TestNet</b>, do
```
cd Purple.PurpleDnsD
dotnet run -testnet -dnsfullnode -dnslistenport=5399 -dnshostname=dns.Purpleplatform.com -dnsnameserver=ns1.dns.Purpleplatform.com -dnsmailbox=admin@Purpleplatform.com
```  

### Command-line arguments

| Argument      | Description                                                                          |
| ------------- | ------------------------------------------------------------------------------------ |
| dnslistenport | The port the Purple DNS Server will listen on                                       |
| dnshostname   | The host name for Purple DNS Server                                                 |
| dnsnameserver | The nameserver host name used as the authoritative domain for the Purple DNS Server |
| dnsmailbox    | The e-mail address used as the administrative point of contact for the domain        |

### NS Record

Given the following settings for the Purple DNS Server:

| Argument      | Value                             |
| ------------- | --------------------------------- |
| dnslistenport | 53                                |
| dnshostname   | purpledns.purpleplatform.com    |
| dnsnameserver | ns.purpledns.purpleplatform.com |

You should have NS and A record in your ISP DNS records for your DNS host domain:

| Type     | Hostname                          | Data                              |
| -------- | --------------------------------- | --------------------------------- |
| NS       | purpledns.purpleplatform.com    | ns.purpledns.purpleplatform.com |
| A        | ns.purpledns.purpleplatform.com | 192.168.1.2                       |

To verify the Purple DNS Server is running with these settings run:

```
dig +qr -p 53 purpledns.purpleplatform.com
```  
or
```
nslookup purpledns.purpleplatform.com
```
