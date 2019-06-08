Imports System.Threading
Imports Algo2TradeCore.Entities
Imports NLog
Imports Algo2TradeCore.Controller
Imports Algo2TradeCore.Calculator
Namespace Adapter
    Public MustInherit Class APIAdapter
        Protected _cts As CancellationTokenSource
        Public Property ParentController As APIStrategyController
        Protected Calculator As APIBrokerageCalculator

#Region "Events/Event handlers"
        Public Event DocumentDownloadComplete()
        Public Event DocumentRetryStatus(ByVal currentTry As Integer, ByVal totalTries As Integer)
        Public Event Heartbeat(ByVal msg As String)
        Public Event WaitingFor(ByVal elapsedSecs As Integer, ByVal totalSecs As Integer, ByVal msg As String)
        'The below functions are needed to allow the derived classes to raise the above two events
        Protected Overridable Sub OnDocumentDownloadComplete()
            RaiseEvent DocumentDownloadComplete()
        End Sub
        Protected Overridable Sub OnDocumentRetryStatus(ByVal currentTry As Integer, ByVal totalTries As Integer)
            RaiseEvent DocumentRetryStatus(currentTry, totalTries)
        End Sub
        Protected Overridable Sub OnHeartbeat(ByVal msg As String)
            RaiseEvent Heartbeat(msg)
        End Sub
        Protected Overridable Sub OnWaitingFor(ByVal elapsedSecs As Integer, ByVal totalSecs As Integer, ByVal msg As String)
            RaiseEvent WaitingFor(elapsedSecs, totalSecs, msg)
        End Sub
#End Region

#Region "Logging and Status Progress"
        Public Shared logger As Logger = LogManager.GetCurrentClassLogger
#End Region

        Public Sub New(ByVal associatedParentController As APIStrategyController,
                       ByVal canceller As CancellationTokenSource)
            Me.ParentController = associatedParentController
            _cts = canceller
        End Sub
        Public MustOverride Function CalculatePLWithBrokerage(ByVal instrument As IInstrument, ByVal buy As Double, ByVal sell As Double, ByVal quantity As Integer) As Decimal
        Public MustOverride Async Function GetAllInstrumentsAsync() As Task(Of IEnumerable(Of IInstrument))
        Public MustOverride Async Function GetAllTradesAsync() As Task(Of IEnumerable(Of ITrade))
        Public MustOverride Async Function GetAllOrdersAsync() As Task(Of IEnumerable(Of IOrder))
        Public MustOverride Async Function GetAllHoldingsAsync() As Task(Of IEnumerable(Of IHolding))
        Public MustOverride Async Function GetAllPositionsAsync() As Task(Of IPositionResponse)
        Public MustOverride Async Function GetUserMarginsAsync() As Task(Of Dictionary(Of Enums.TypeOfExchage, IUserMargin))
        Public MustOverride Async Function GetAllQuotesAsync(ByVal instruments As IEnumerable(Of IInstrument)) As Task(Of IEnumerable(Of IQuote))
        Public MustOverride Sub SetAPIAccessToken(ByVal apiAccessToken As String)
        Public MustOverride Function CreateSingleInstrument(ByVal supportedTradingSymbol As String, ByVal instrumentToken As UInteger, ByVal sampleInstrument As IInstrument) As IInstrument
        Public MustOverride Async Function ModifyStoplossOrderAsync(ByVal orderId As String, ByVal triggerPrice As Decimal) As Task(Of Dictionary(Of String, Object))
        Public MustOverride Async Function ModifyTargetOrderAsync(ByVal orderId As String, ByVal price As Decimal) As Task(Of Dictionary(Of String, Object))
        Public MustOverride Async Function CancelBOOrderAsync(ByVal orderId As String, ByVal parentOrderID As String) As Task(Of Dictionary(Of String, Object))
        Public MustOverride Async Function CancelCOOrderAsync(ByVal orderId As String, ByVal parentOrderID As String) As Task(Of Dictionary(Of String, Object))
        Public MustOverride Async Function CancelRegularOrderAsync(ByVal orderId As String, ByVal parentOrderID As String) As Task(Of Dictionary(Of String, Object))
        Public MustOverride Async Function PlaceBOLimitMISOrderAsync(ByVal tradeExchange As String, ByVal tradingSymbol As String, ByVal transaction As IOrder.TypeOfTransaction, ByVal quantity As Integer, ByVal price As Decimal, ByVal squareOffValue As Decimal, ByVal stopLossValue As Decimal, ByVal tag As String) As Task(Of Dictionary(Of String, Object))
        Public MustOverride Async Function PlaceBOSLMISOrderAsync(ByVal tradeExchange As String, ByVal tradingSymbol As String, ByVal transaction As IOrder.TypeOfTransaction, ByVal quantity As Integer, ByVal price As Decimal, ByVal triggerPrice As Decimal, ByVal squareOffValue As Decimal, ByVal stopLossValue As Decimal, ByVal tag As String) As Task(Of Dictionary(Of String, Object))
        Public MustOverride Async Function PlaceCOMarketMISOrderAsync(ByVal tradeExchange As String, ByVal tradingSymbol As String, ByVal transaction As IOrder.TypeOfTransaction, ByVal quantity As Integer, ByVal triggerPrice As Decimal, ByVal tag As String) As Task(Of Dictionary(Of String, Object))
        Public MustOverride Async Function PlaceRegularMarketMISOrderAsync(ByVal tradeExchange As String, ByVal tradingSymbol As String, ByVal transaction As IOrder.TypeOfTransaction, ByVal quantity As Integer, ByVal tag As String) As Task(Of Dictionary(Of String, Object))
        Public MustOverride Async Function PlaceRegularLimitMISOrderAsync(ByVal tradeExchange As String, ByVal tradingSymbol As String, ByVal transaction As IOrder.TypeOfTransaction, ByVal quantity As Integer, ByVal price As Decimal, ByVal tag As String) As Task(Of Dictionary(Of String, Object))
        Public MustOverride Async Function PlaceRegularSLMMISOrderAsync(ByVal tradeExchange As String, ByVal tradingSymbol As String, ByVal transaction As IOrder.TypeOfTransaction, ByVal quantity As Integer, ByVal triggerPrice As Decimal, ByVal tag As String) As Task(Of Dictionary(Of String, Object))
        Public MustOverride Async Function PlaceRegularMarketCNCOrderAsync(ByVal tradeExchange As String, ByVal tradingSymbol As String, ByVal transaction As IOrder.TypeOfTransaction, ByVal quantity As Integer, ByVal tag As String) As Task(Of Dictionary(Of String, Object))
        Public Enum ExecutionCommands
            GetPositions = 1
            GetQuotes
            GetHoldings
            PlaceOrder
            ModifyOrderQuantity
            ModifyOrderPrice
            ModifyTargetOrderPrice
            ModifySLOrderPrice
            CancelOrder
            GetOrderHistory
            GetOrders
            GetOrderTrades
            GetInstruments
            GetUserMargins
            InvalidateAccessToken
            GenerateSession
            None
        End Enum

    End Class
End Namespace