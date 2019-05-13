Namespace Calculator
    Public Class ZerodhaBrokerageAttributes
        Implements IBrokerageAttributes

        Public Property Buy As Decimal Implements IBrokerageAttributes.Buy
        Public Property Sell As Decimal Implements IBrokerageAttributes.Sell
        Public Property Quantity As Integer Implements IBrokerageAttributes.Quantity
        Public Property Multiplier As Decimal
        Public Property Turnover As Decimal
        Public Property Brokerage As Decimal
        Public Property STT As Integer
        Public Property CTT As Decimal
        Public Property ExchangeFees As Decimal
        Public Property Clearing As Decimal
        Public Property GST As Decimal

        Private _SEBI As Decimal
        Public Property SEBI As Decimal
            Get
                _SEBI = 0.000001 * Turnover
                Return _SEBI
            End Get
            Set(value As Decimal)
                _SEBI = value
            End Set
        End Property
        Public Property TotalTax As Decimal

        Private _BreakevenPoints As Decimal
        Public Property BreakevenPoints As Decimal
            Get
                _BreakevenPoints = TotalTax / (Quantity * Multiplier)
                Return _BreakevenPoints
            End Get
            Set(value As Decimal)
                _BreakevenPoints = value
            End Set
        End Property

        Private _NetProfitLoss As Decimal
        Public Property NetProfitLoss As Decimal Implements IBrokerageAttributes.NetProfitLoss
            Get
                _NetProfitLoss = ((Sell - Buy) * Quantity * Multiplier) - TotalTax
                Return _NetProfitLoss
            End Get
            Set(value As Decimal)
                _NetProfitLoss = value
            End Set
        End Property
    End Class
End Namespace
