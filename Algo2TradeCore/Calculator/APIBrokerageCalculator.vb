Imports System.Threading
Imports Algo2TradeCore.Controller
Imports Algo2TradeCore.Entities

Namespace Calculator
    Public MustInherit Class APIBrokerageCalculator

        Protected _cts As CancellationTokenSource
        Property ParentController As APIStrategyController
        Public Sub New(ByVal associatedParentController As APIStrategyController, canceller As CancellationTokenSource)
            Me.ParentController = associatedParentController
            _cts = canceller
        End Sub
        Public MustOverride Function GetIntradayEquityBrokerage(ByVal buy As Decimal, ByVal sell As Decimal, ByVal quantity As Integer) As IBrokerageAttributes
        Public MustOverride Function GetDeliveryEquityBrokerage(ByVal buy As Decimal, ByVal sell As Decimal, ByVal quantity As Integer) As IBrokerageAttributes
        Public MustOverride Function GetIntradayEquityFuturesBrokerage(ByVal buy As Decimal, ByVal sell As Decimal, ByVal quantity As Integer) As IBrokerageAttributes
        Public MustOverride Function GetIntradayCommodityFuturesBrokerage(ByVal instrument As IInstrument, ByVal buy As Decimal, ByVal sell As Decimal, ByVal quantity As Integer) As IBrokerageAttributes
        Public MustOverride Function GetIntradayEquityOptionsBrokerage(ByVal buy As Decimal, ByVal sell As Decimal, ByVal quantity As Integer) As IBrokerageAttributes
        Public MustOverride Function GetIntradayCurrencyFuturesBrokerage(ByVal buy As Decimal, ByVal sell As Decimal, ByVal quantity As Integer) As IBrokerageAttributes
        Public MustOverride Function GetIntradayCurrencyOptionsBrokerage(ByVal strikePrice As Decimal, ByVal buyPremium As Decimal, ByVal sellPremium As Decimal, ByVal quantity As Integer) As IBrokerageAttributes

    End Class
End Namespace