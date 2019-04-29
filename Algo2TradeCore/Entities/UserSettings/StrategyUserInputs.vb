Namespace Entities.UserSettings
    <Serializable>
    Public Class StrategyUserInputs
        Private _TradeStartTime As Date
        Public Property TradeStartTime As Date
            Get
                Return New Date(Now.Year, Now.Month, Now.Day, _TradeStartTime.Hour, _TradeStartTime.Minute, _TradeStartTime.Second)
            End Get
            Set(value As Date)
                _TradeStartTime = value
            End Set
        End Property

        Private _LastTradeEntryTime As Date
        Public Property LastTradeEntryTime As Date
            Get
                Return New Date(Now.Year, Now.Month, Now.Day, _LastTradeEntryTime.Hour, _LastTradeEntryTime.Minute, _LastTradeEntryTime.Second)
            End Get
            Set(value As Date)
                _LastTradeEntryTime = value
            End Set
        End Property

        Private _EODExitTime As Date
        Public Property EODExitTime As Date
            Get
                Return New Date(Now.Year, Now.Month, Now.Day, _EODExitTime.Hour, _EODExitTime.Minute, _EODExitTime.Second)
            End Get
            Set(value As Date)
                _EODExitTime = value
            End Set
        End Property

        Public Property SignalTimeFrame As Integer
        Public Property TargetMultiplier As Decimal
        Public Property MaxLossPercentagePerDay As Decimal
        Public Property MaxProfitPercentagePerDay As Decimal
    End Class
End Namespace
