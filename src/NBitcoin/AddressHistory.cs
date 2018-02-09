using System;
using System.Collections.Generic;

namespace NBitcoin
{
    public class AddressHistory : IBitcoinSerializable
    {
        static readonly Money NullMoney = new Money(0);

        private string _Address;
        private Money _Balance = NullMoney;
        private List<AddressTransaction> _Transactions = new List<AddressTransaction>();
        private int _LastModifiedBlockHeight;

        public AddressHistory()
        {
        }

        public AddressHistory(BitcoinAddress address)
        {
            this._Address = address.ToString();
        }

        public string Address
        {
            get { return _Address; }
            set { _Address = value; }
        }

        public Money Balance
        {
            get { return _Balance; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                _Balance = value;
            }
        }

        public List<AddressTransaction> Transactions
        {
            get { return _Transactions; }
        }

        public int LastModifiedBlockHeight
        {
            get { return _LastModifiedBlockHeight; }
            set { _LastModifiedBlockHeight = value; }
        }

        public void ReadWrite(BitcoinStream stream)
        {
            long value = this._Balance.Satoshi;
            stream.ReadWrite(ref value);
            if (!stream.Serializing)
                this._Balance = new Money(value);
            stream.ReadWrite(ref this._LastModifiedBlockHeight);
            stream.ReadWrite(ref this._Transactions);
        }
    }

    public class AddressTransaction : IBitcoinSerializable
    {
        static readonly Money NullMoney = new Money(0);

        private uint256 _TxId;
        private Money _Value = NullMoney;
        private TransactionDirection _Direction;

        public AddressTransaction()
        {
        }

        public AddressTransaction(uint256 txid, Money value, TransactionDirection direction)
        {
            this._TxId = txid;
            this._Value = value;
            this._Direction = direction;
        }

        public uint256 TxId
        {
            get { return _TxId; }
            set { _TxId = value; }
        }

        public Money Value
        {
            get { return _Value; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                _Value = value;
            }
        }

        public TransactionDirection Direction
        {
            get { return _Direction; }
            set { _Direction = value; }
        }

        public void ReadWrite(BitcoinStream stream)
        {
            stream.ReadWrite(ref this._TxId);

            long value = this._Value.Satoshi;
            stream.ReadWrite(ref value);
            if (!stream.Serializing)
                this._Value = new Money(value);

            int direction = (int)this._Direction;
            stream.ReadWrite(ref direction);
            if (!stream.Serializing)
                this._Direction = (TransactionDirection)direction;
        }
    }

    [Flags]
    public enum TransactionDirection : int
    {
        Vin = 0,
        Vout = 1
    }
}