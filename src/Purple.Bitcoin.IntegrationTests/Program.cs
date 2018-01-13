namespace Purple.Bitcoin.IntegrationTests
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            new WalletTests().CanMineAndSendToAddress();
        }
    }
}
