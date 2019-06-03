Imports System.Threading
Imports Algo2TradeCore.Entities
Imports Algo2TradeCore.Calculator
Imports KiteConnect
Imports NLog
Imports Algo2TradeCore.Controller
Imports Algo2TradeCore.Exceptions

Namespace Adapter
    Public Class ZerodhaAdapter
        Inherits APIAdapter

#Region "Logging and Status Progress"
        Public Shared Shadows logger As Logger = LogManager.GetCurrentClassLogger
#End Region

        Protected _Kite As Kite
        Public Sub New(ByVal associatedParentController As ZerodhaStrategyController,
                        ByVal canceller As CancellationTokenSource)
            MyBase.New(associatedParentController, canceller)
            _Kite = New Kite(APIKey:=CType(associatedParentController.APIConnection, ZerodhaConnection).ZerodhaUser.APIKey,
                             AccessToken:=CType(associatedParentController.APIConnection, ZerodhaConnection).AccessToken,
                             Debug:=False)
            _Kite.SetSessionExpiryHook(AddressOf associatedParentController.OnSessionExpireAsync)
            Calculator = New ZerodhaBrokerageCalculator(Me.ParentController, canceller)
        End Sub

#Region "Brokerage Calculator"
        Public Overrides Function CalculatePLWithBrokerage(ByVal intrument As IInstrument,
                                                            ByVal buy As Double,
                                                            ByVal sell As Double,
                                                            ByVal quantity As Integer) As Decimal
            Dim ret As Decimal = Nothing
            Dim brokerageAttributes As IBrokerageAttributes = Nothing
            Select Case intrument.RawExchange
                Case "NSE"
                    brokerageAttributes = Calculator.GetIntradayEquityBrokerage(buy, sell, quantity)
                Case "NFO"
                    brokerageAttributes = Calculator.GetIntradayEquityFuturesBrokerage(buy, sell, quantity)
                Case "MCX"
                    brokerageAttributes = Calculator.GetIntradayCommodityFuturesBrokerage(intrument, buy, sell, quantity)
                Case "CDS"
                    brokerageAttributes = Calculator.GetIntradayCurrencyFuturesBrokerage(buy, sell, quantity)
                Case Else
                    Throw New NotImplementedException("Calculator not implemented")
            End Select
            ret = brokerageAttributes.NetProfitLoss
            Return ret
        End Function
#End Region

#Region "Access Token"
        Public Overrides Sub SetAPIAccessToken(ByVal apiAccessToken As String)
            'logger.Debug("SetAPIAccessToken, apiAccessToken:{0}", apiAccessToken)
            _Kite.SetAccessToken(apiAccessToken)
        End Sub
#End Region

#Region "All Instruments"
        Public Overrides Async Function GetAllInstrumentsAsync() As Task(Of IEnumerable(Of IInstrument))
            logger.Debug("GetAllInstrumentsAsync, parameters:Nothing")
            Dim ret As List(Of ZerodhaInstrument) = Nothing
            Dim execCommand As ExecutionCommands = ExecutionCommands.GetInstruments

            _cts.Token.ThrowIfCancellationRequested()
            Dim tempAllRet As Dictionary(Of String, Object) = Nothing
            Try
                tempAllRet = Await ExecuteCommandAsync(execCommand, Nothing).ConfigureAwait(False)
            Catch tex As TokenException
                Throw New ZerodhaBusinessException(tex.Message, tex, AdapterBusinessException.TypeOfException.TokenException)
            Catch gex As GeneralException
                Throw New ZerodhaBusinessException(gex.Message, gex, AdapterBusinessException.TypeOfException.GeneralException)
            Catch pex As PermissionException
                Throw New ZerodhaBusinessException(pex.Message, pex, AdapterBusinessException.TypeOfException.PermissionException)
            Catch oex As OrderException
                Throw New ZerodhaBusinessException(oex.Message, oex, AdapterBusinessException.TypeOfException.OrderException)
            Catch iex As InputException
                Throw New ZerodhaBusinessException(iex.Message, iex, AdapterBusinessException.TypeOfException.InputException)
            Catch dex As DataException
                Throw New ZerodhaBusinessException(dex.Message, dex, AdapterBusinessException.TypeOfException.DataException)
            Catch nex As NetworkException
                Throw New ZerodhaBusinessException(nex.Message, nex, AdapterBusinessException.TypeOfException.NetworkException)
            Catch ex As Exception
                Throw ex
            End Try
            _cts.Token.ThrowIfCancellationRequested()

            Dim tempRet As Object = Nothing
            If tempAllRet IsNot Nothing AndAlso tempAllRet.ContainsKey(execCommand.ToString) Then
                tempRet = tempAllRet(execCommand.ToString)
                If tempRet IsNot Nothing Then
                    Dim errorMessage As String = ParentController.GetErrorResponse(tempRet)
                    If errorMessage IsNot Nothing Then
                        Throw New ApplicationException(errorMessage)
                    End If
                Else
                    Throw New ApplicationException(String.Format("Zerodha command execution did not return anything, command:{0}", execCommand.ToString))
                End If
            Else
                Throw New ApplicationException(String.Format("Relevant command was fired but not detected in the response, command:{0}", execCommand.ToString))
            End If

            If tempRet.GetType = GetType(List(Of Instrument)) Then
                OnHeartbeat(String.Format("Creating Zerodha instrument collection from API instruments, count:{0}", tempRet.count))
                Dim zerodhaReturedInstruments As List(Of Instrument) = CType(tempRet, List(Of Instrument))
                For Each runningInstrument As Instrument In zerodhaReturedInstruments
                    _cts.Token.ThrowIfCancellationRequested()
                    If ret Is Nothing Then ret = New List(Of ZerodhaInstrument)
                    ret.Add(New ZerodhaInstrument(Me.ParentController, runningInstrument.InstrumentToken) With {.WrappedInstrument = runningInstrument})
                Next
            Else
                Throw New ApplicationException(String.Format("Zerodha command execution did not return any list of instrument, command:{0}", execCommand.ToString))
            End If
            Return ret
        End Function
        Public Overrides Function CreateSingleInstrument(ByVal supportedTradingSymbol As String, ByVal instrumentToken As UInteger, ByVal sampleInstrument As IInstrument) As IInstrument
            Dim ret As ZerodhaInstrument = Nothing
            If supportedTradingSymbol IsNot Nothing Then
                Dim dummyInstrument As Instrument = New Instrument With
                    {
                    .TradingSymbol = supportedTradingSymbol,
                    .Exchange = sampleInstrument.RawExchange,
                    .Expiry = Nothing,
                    .ExchangeToken = instrumentToken,
                    .InstrumentToken = instrumentToken,
                    .InstrumentType = Nothing,
                    .LastPrice = 0,
                    .LotSize = 0,
                    .Name = supportedTradingSymbol,
                    .Segment = Nothing,
                    .TickSize = sampleInstrument.TickSize
                    }

                ret = New ZerodhaInstrument(Me.ParentController, dummyInstrument.InstrumentToken) With {.WrappedInstrument = dummyInstrument}
            End If
            Return ret
        End Function
#End Region

#Region "Quotes"
        Public Overrides Async Function GetAllQuotesAsync(ByVal instruments As IEnumerable(Of IInstrument)) As Task(Of IEnumerable(Of IQuote))
            'logger.Debug("GetAllQuotes, parameters:{0}", Utils.JsonSerialize(instruments))
            Dim ret As List(Of ZerodhaQuote) = Nothing
            Dim execCommand As ExecutionCommands = ExecutionCommands.GetQuotes

            _cts.Token.ThrowIfCancellationRequested()
            Dim tempAllRet As Dictionary(Of String, Object) = Nothing
            Try
                tempAllRet = Await ExecuteCommandAsync(execCommand, New Dictionary(Of String, Object) From {{"instruments", instruments}}).ConfigureAwait(False)
            Catch tex As TokenException
                Throw New ZerodhaBusinessException(tex.Message, tex, AdapterBusinessException.TypeOfException.TokenException)
            Catch gex As GeneralException
                Throw New ZerodhaBusinessException(gex.Message, gex, AdapterBusinessException.TypeOfException.GeneralException)
            Catch pex As PermissionException
                Throw New ZerodhaBusinessException(pex.Message, pex, AdapterBusinessException.TypeOfException.PermissionException)
            Catch oex As OrderException
                Throw New ZerodhaBusinessException(oex.Message, oex, AdapterBusinessException.TypeOfException.OrderException)
            Catch iex As InputException
                Throw New ZerodhaBusinessException(iex.Message, iex, AdapterBusinessException.TypeOfException.InputException)
            Catch dex As DataException
                Throw New ZerodhaBusinessException(dex.Message, dex, AdapterBusinessException.TypeOfException.DataException)
            Catch nex As NetworkException
                Throw New ZerodhaBusinessException(nex.Message, nex, AdapterBusinessException.TypeOfException.NetworkException)
            Catch ex As Exception
                Throw ex
            End Try
            _cts.Token.ThrowIfCancellationRequested()

            Dim tempRet As Object = Nothing
            If tempAllRet IsNot Nothing AndAlso tempAllRet.ContainsKey(execCommand.ToString) Then
                tempRet = tempAllRet(execCommand.ToString)
                If tempRet IsNot Nothing Then
                    Dim errorMessage As String = ParentController.GetErrorResponse(tempRet)
                    If errorMessage IsNot Nothing Then
                        Throw New ApplicationException(errorMessage)
                    End If
                Else
                    Throw New ApplicationException(String.Format("Zerodha command execution did not return anything, command:{0}", execCommand.ToString))
                End If
            Else
                Throw New ApplicationException(String.Format("Relevant command was fired but not detected in the response, command:{0}", execCommand.ToString))
            End If

            If tempRet.GetType = GetType(Dictionary(Of String, Quote)) Then
                OnHeartbeat(String.Format("Creating Zerodha quote collection from API quotes, count:{0}", tempRet.count))
                Dim zerodhaReturedQuotes As Dictionary(Of String, Quote) = CType(tempRet, Dictionary(Of String, Quote))
                For Each runningQuote In zerodhaReturedQuotes
                    _cts.Token.ThrowIfCancellationRequested()
                    If ret Is Nothing Then ret = New List(Of ZerodhaQuote)
                    ret.Add(New ZerodhaQuote() With {.WrappedQuote = runningQuote.Value})
                Next
            Else
                Throw New ApplicationException(String.Format("Zerodha command execution did not return any list of quotes, command:{0}", execCommand.ToString))
            End If
            Return ret
        End Function
#End Region

#Region "Trades & Orders"
        Public Overrides Async Function GetAllTradesAsync() As Task(Of IEnumerable(Of ITrade))
            'logger.Debug("GetAllTradesAsync, parameters:Nothing")
            Dim ret As List(Of ZerodhaTrade) = Nothing
            Dim execCommand As ExecutionCommands = ExecutionCommands.GetOrderTrades
            _cts.Token.ThrowIfCancellationRequested()
            Dim tempAllRet As Dictionary(Of String, Object) = Nothing
            Try
                tempAllRet = Await ExecuteCommandAsync(execCommand, Nothing).ConfigureAwait(False)
            Catch tex As TokenException
                Throw New ZerodhaBusinessException(tex.Message, tex, AdapterBusinessException.TypeOfException.TokenException)
            Catch gex As GeneralException
                Throw New ZerodhaBusinessException(gex.Message, gex, AdapterBusinessException.TypeOfException.GeneralException)
            Catch pex As PermissionException
                Throw New ZerodhaBusinessException(pex.Message, pex, AdapterBusinessException.TypeOfException.PermissionException)
            Catch oex As OrderException
                Throw New ZerodhaBusinessException(oex.Message, oex, AdapterBusinessException.TypeOfException.OrderException)
            Catch iex As InputException
                Throw New ZerodhaBusinessException(iex.Message, iex, AdapterBusinessException.TypeOfException.InputException)
            Catch dex As DataException
                Throw New ZerodhaBusinessException(dex.Message, dex, AdapterBusinessException.TypeOfException.DataException)
            Catch nex As NetworkException
                Throw New ZerodhaBusinessException(nex.Message, nex, AdapterBusinessException.TypeOfException.NetworkException)
            Catch ex As Exception
                Throw ex
            End Try
            _cts.Token.ThrowIfCancellationRequested()

            Dim tempRet As Object = Nothing
            If tempAllRet IsNot Nothing AndAlso tempAllRet.ContainsKey(execCommand.ToString) Then
                tempRet = tempAllRet(execCommand.ToString)
                If tempRet IsNot Nothing Then
                    Dim errorMessage As String = ParentController.GetErrorResponse(tempRet)
                    If errorMessage IsNot Nothing Then
                        Throw New ApplicationException(errorMessage)
                    End If
                Else
                    Throw New ApplicationException(String.Format("Zerodha command execution did not return anything, command:{0}", execCommand.ToString))
                End If
            Else
                Throw New ApplicationException(String.Format("Relevant command was fired but not detected in the response, command:{0}", execCommand.ToString))
            End If

            If tempRet.GetType = GetType(List(Of Trade)) Then
                If tempRet.count > 0 Then
                    OnHeartbeat(String.Format("Creating Zerodha trade collection from API trades, count:{0}", tempRet.count))
                    Dim zerodhaReturedTrades As List(Of Trade) = CType(tempRet, List(Of Trade))
                    For Each runningTrade As Trade In zerodhaReturedTrades
                        _cts.Token.ThrowIfCancellationRequested()
                        If ret Is Nothing Then ret = New List(Of ZerodhaTrade)
                        ret.Add(New ZerodhaTrade With {.WrappedTrade = runningTrade})
                    Next
                Else
                    OnHeartbeat(String.Format("Zerodha command execution did not return any list of trade, command:{0}", execCommand.ToString))
                    If ret Is Nothing Then ret = New List(Of ZerodhaTrade)
                End If
            Else
                Throw New ApplicationException(String.Format("Zerodha command execution did not return any list of trade, command:{0}", execCommand.ToString))
            End If
            Return ret
        End Function
        Public Overrides Async Function GetAllOrdersAsync() As Task(Of IEnumerable(Of IOrder))
            'logger.Debug("GetAllOrdersAsync, parameters:Nothing")
            Dim ret As List(Of ZerodhaOrder) = Nothing
            Dim execCommand As ExecutionCommands = ExecutionCommands.GetOrders
            _cts.Token.ThrowIfCancellationRequested()
            Dim tempAllRet As Dictionary(Of String, Object) = Nothing
            Try
                tempAllRet = Await ExecuteCommandAsync(execCommand, Nothing).ConfigureAwait(False)
            Catch tex As TokenException
                Throw New ZerodhaBusinessException(tex.Message, tex, AdapterBusinessException.TypeOfException.TokenException)
            Catch gex As GeneralException
                Throw New ZerodhaBusinessException(gex.Message, gex, AdapterBusinessException.TypeOfException.GeneralException)
            Catch pex As PermissionException
                Throw New ZerodhaBusinessException(pex.Message, pex, AdapterBusinessException.TypeOfException.PermissionException)
            Catch oex As OrderException
                Throw New ZerodhaBusinessException(oex.Message, oex, AdapterBusinessException.TypeOfException.OrderException)
            Catch iex As InputException
                Throw New ZerodhaBusinessException(iex.Message, iex, AdapterBusinessException.TypeOfException.InputException)
            Catch dex As DataException
                Throw New ZerodhaBusinessException(dex.Message, dex, AdapterBusinessException.TypeOfException.DataException)
            Catch nex As NetworkException
                Throw New ZerodhaBusinessException(nex.Message, nex, AdapterBusinessException.TypeOfException.NetworkException)
            Catch ex As Exception
                Throw ex
            End Try
            _cts.Token.ThrowIfCancellationRequested()

            Dim tempRet As Object = Nothing
            If tempAllRet IsNot Nothing AndAlso tempAllRet.ContainsKey(execCommand.ToString) Then
                tempRet = tempAllRet(execCommand.ToString)
                If tempRet IsNot Nothing Then
                    Dim errorMessage As String = ParentController.GetErrorResponse(tempRet)
                    If errorMessage IsNot Nothing Then
                        Throw New ApplicationException(errorMessage)
                    End If
                Else
                    Throw New ApplicationException(String.Format("Zerodha command execution did not return anything, command:{0}", execCommand.ToString))
                End If
            Else
                Throw New ApplicationException(String.Format("Relevant command was fired but not detected in the response, command:{0}", execCommand.ToString))
            End If

            If tempRet.GetType = GetType(List(Of Order)) Then
                If tempRet.count > 0 Then
                    'OnHeartbeat(String.Format("Creating Zerodha order collection from API orders, count:{0}", tempRet.count))
                    'logger.Debug(String.Format("Creating Zerodha order collection from API orders, count:{0}", tempRet.count))
                    Dim zerodhaReturedOrders As List(Of Order) = CType(tempRet, List(Of Order))
                    For Each runningOrder As Order In zerodhaReturedOrders
                        _cts.Token.ThrowIfCancellationRequested()
                        If ret Is Nothing Then ret = New List(Of ZerodhaOrder)
                        ret.Add(New ZerodhaOrder With {.WrappedOrder = runningOrder})
                    Next
                    'Else
                    'OnHeartbeat(String.Format("Zerodha command execution did not return any list of order, command:{0}", execCommand.ToString))
                    'logger.Debug(String.Format("Zerodha command execution did not return any list of order, command:{0}", execCommand.ToString))
                End If
            Else
                Throw New ApplicationException(String.Format("Zerodha command execution did not return any list of order, command:{0}", execCommand.ToString))
            End If
            Return ret
        End Function
#End Region

#Region "Margin"
        Public Overrides Async Function GetUserMarginsAsync() As Task(Of Dictionary(Of Enums.TypeOfExchage, IUserMargin))
            'logger.Debug("GetAllOrdersAsync, parameters:Nothing")
            Dim ret As Dictionary(Of Enums.TypeOfExchage, IUserMargin) = Nothing
            Dim execCommand As ExecutionCommands = ExecutionCommands.GetUserMargins
            _cts.Token.ThrowIfCancellationRequested()
            Dim tempAllRet As Dictionary(Of String, Object) = Nothing
            Try
                tempAllRet = Await ExecuteCommandAsync(execCommand, Nothing).ConfigureAwait(False)
            Catch tex As TokenException
                Throw New ZerodhaBusinessException(tex.Message, tex, AdapterBusinessException.TypeOfException.TokenException)
            Catch gex As GeneralException
                Throw New ZerodhaBusinessException(gex.Message, gex, AdapterBusinessException.TypeOfException.GeneralException)
            Catch pex As PermissionException
                Throw New ZerodhaBusinessException(pex.Message, pex, AdapterBusinessException.TypeOfException.PermissionException)
            Catch oex As OrderException
                Throw New ZerodhaBusinessException(oex.Message, oex, AdapterBusinessException.TypeOfException.OrderException)
            Catch iex As InputException
                Throw New ZerodhaBusinessException(iex.Message, iex, AdapterBusinessException.TypeOfException.InputException)
            Catch dex As DataException
                Throw New ZerodhaBusinessException(dex.Message, dex, AdapterBusinessException.TypeOfException.DataException)
            Catch nex As NetworkException
                Throw New ZerodhaBusinessException(nex.Message, nex, AdapterBusinessException.TypeOfException.NetworkException)
            Catch ex As Exception
                Throw ex
            End Try
            _cts.Token.ThrowIfCancellationRequested()

            Dim tempRet As Object = Nothing
            If tempAllRet IsNot Nothing AndAlso tempAllRet.ContainsKey(execCommand.ToString) Then
                tempRet = tempAllRet(execCommand.ToString)
                If tempRet IsNot Nothing Then
                    Dim errorMessage As String = ParentController.GetErrorResponse(tempRet)
                    If errorMessage IsNot Nothing Then
                        Throw New ApplicationException(errorMessage)
                    End If
                Else
                    Throw New ApplicationException(String.Format("Zerodha command execution did not return anything, command:{0}", execCommand.ToString))
                End If
            Else
                Throw New ApplicationException(String.Format("Relevant command was fired but not detected in the response, command:{0}", execCommand.ToString))
            End If

            If tempRet.GetType = GetType(UserMarginsResponse) Then
                'OnHeartbeat(String.Format("Creating Zerodha order collection from API orders, count:{0}", tempRet.count))
                logger.Debug(String.Format("Creating IBussinessUserMargin from API User Margin", Utils.JsonSerialize(tempRet)))
                Dim zerodhaReturedUserMarginResponse As UserMarginsResponse = CType(tempRet, UserMarginsResponse)
                logger.Debug(Utilities.Strings.JsonSerialize(zerodhaReturedUserMarginResponse))
                Dim equityMargin As New ZerodhaUserMargin With
                    {.WrappedUserMargin = zerodhaReturedUserMarginResponse.Equity}
                Dim commodityMargin As New ZerodhaUserMargin With
                    {.WrappedUserMargin = zerodhaReturedUserMarginResponse.Commodity}
                If ret Is Nothing Then ret = New Dictionary(Of Enums.TypeOfExchage, IUserMargin)
                ret.Add(Enums.TypeOfExchage.NSE, equityMargin)
                ret.Add(Enums.TypeOfExchage.MCX, commodityMargin)
            Else
                Throw New ApplicationException(String.Format("Zerodha command execution did not return any User margin, command:{0}", execCommand.ToString))
            End If
            Return ret
        End Function
#End Region

#Region "Modify Order"
        Public Overrides Async Function ModifyStoplossOrderAsync(ByVal orderId As String, ByVal triggerPrice As Decimal) As Task(Of Dictionary(Of String, Object))
            Dim ret As Dictionary(Of String, Object) = Nothing
            Dim execCommand As ExecutionCommands = ExecutionCommands.ModifySLOrderPrice
            _cts.Token.ThrowIfCancellationRequested()
            Dim tradeParameters As New Dictionary(Of String, Object) From {{"OrderId", orderId}, {"TriggerPrice", triggerPrice}}
            Dim tempAllRet As Dictionary(Of String, Object) = Nothing
            Try
                tempAllRet = Await ExecuteCommandAsync(execCommand, tradeParameters).ConfigureAwait(False)
            Catch tex As TokenException
                Throw New ZerodhaBusinessException(tex.Message, tex, AdapterBusinessException.TypeOfException.TokenException)
            Catch gex As GeneralException
                Throw New ZerodhaBusinessException(gex.Message, gex, AdapterBusinessException.TypeOfException.GeneralException)
            Catch pex As PermissionException
                Throw New ZerodhaBusinessException(pex.Message, pex, AdapterBusinessException.TypeOfException.PermissionException)
            Catch oex As OrderException
                Throw New ZerodhaBusinessException(oex.Message, oex, AdapterBusinessException.TypeOfException.OrderException)
            Catch iex As InputException
                Throw New ZerodhaBusinessException(iex.Message, iex, AdapterBusinessException.TypeOfException.InputException)
            Catch dex As DataException
                Throw New ZerodhaBusinessException(dex.Message, dex, AdapterBusinessException.TypeOfException.DataException)
            Catch nex As NetworkException
                Throw New ZerodhaBusinessException(nex.Message, nex, AdapterBusinessException.TypeOfException.NetworkException)
            Catch ex As Exception
                Throw ex
            End Try
            _cts.Token.ThrowIfCancellationRequested()

            Dim tempRet As Object = Nothing
            If tempAllRet IsNot Nothing AndAlso tempAllRet.ContainsKey(execCommand.ToString) Then
                tempRet = tempAllRet(execCommand.ToString)
                If tempRet IsNot Nothing Then
                    Dim errorMessage As String = ParentController.GetErrorResponse(tempRet)
                    If errorMessage IsNot Nothing Then
                        Throw New ApplicationException(errorMessage)
                    End If
                Else
                    Throw New ApplicationException(String.Format("Zerodha command execution did not return anything, command:{0}", execCommand.ToString))
                End If
            Else
                Throw New ApplicationException(String.Format("Relevant command was fired but not detected in the response, command:{0}", execCommand.ToString))
            End If

            If tempRet.GetType = GetType(Dictionary(Of String, Object)) Then
                OnHeartbeat(String.Format("Modify Order successful, details:{0}", Utils.JsonSerialize(tempRet)))
                ret = CType(tempRet, Dictionary(Of String, Object))
            Else
                Throw New ApplicationException(String.Format("Zerodha command execution did not return anything, command:{0}", execCommand.ToString))
            End If
            Return ret
        End Function
        Public Overrides Async Function ModifyTargetOrderAsync(ByVal orderId As String, ByVal price As Decimal) As Task(Of Dictionary(Of String, Object))
            Dim ret As Dictionary(Of String, Object) = Nothing
            Dim execCommand As ExecutionCommands = ExecutionCommands.ModifyTargetOrderPrice
            _cts.Token.ThrowIfCancellationRequested()
            Dim tradeParameters As New Dictionary(Of String, Object) From {{"OrderId", orderId}, {"Price", price}}
            Dim tempAllRet As Dictionary(Of String, Object) = Nothing
            Try
                tempAllRet = Await ExecuteCommandAsync(execCommand, tradeParameters).ConfigureAwait(False)
            Catch tex As TokenException
                Throw New ZerodhaBusinessException(tex.Message, tex, AdapterBusinessException.TypeOfException.TokenException)
            Catch gex As GeneralException
                Throw New ZerodhaBusinessException(gex.Message, gex, AdapterBusinessException.TypeOfException.GeneralException)
            Catch pex As PermissionException
                Throw New ZerodhaBusinessException(pex.Message, pex, AdapterBusinessException.TypeOfException.PermissionException)
            Catch oex As OrderException
                Throw New ZerodhaBusinessException(oex.Message, oex, AdapterBusinessException.TypeOfException.OrderException)
            Catch iex As InputException
                Throw New ZerodhaBusinessException(iex.Message, iex, AdapterBusinessException.TypeOfException.InputException)
            Catch dex As DataException
                Throw New ZerodhaBusinessException(dex.Message, dex, AdapterBusinessException.TypeOfException.DataException)
            Catch nex As NetworkException
                Throw New ZerodhaBusinessException(nex.Message, nex, AdapterBusinessException.TypeOfException.NetworkException)
            Catch ex As Exception
                Throw ex
            End Try
            _cts.Token.ThrowIfCancellationRequested()

            Dim tempRet As Object = Nothing
            If tempAllRet IsNot Nothing AndAlso tempAllRet.ContainsKey(execCommand.ToString) Then
                tempRet = tempAllRet(execCommand.ToString)
                If tempRet IsNot Nothing Then
                    Dim errorMessage As String = ParentController.GetErrorResponse(tempRet)
                    If errorMessage IsNot Nothing Then
                        Throw New ApplicationException(errorMessage)
                    End If
                Else
                    Throw New ApplicationException(String.Format("Zerodha command execution did not return anything, command:{0}", execCommand.ToString))
                End If
            Else
                Throw New ApplicationException(String.Format("Relevant command was fired but not detected in the response, command:{0}", execCommand.ToString))
            End If

            If tempRet.GetType = GetType(Dictionary(Of String, Object)) Then
                OnHeartbeat(String.Format("Modify Order successful, details:{0}", Utils.JsonSerialize(tempRet)))
                ret = CType(tempRet, Dictionary(Of String, Object))
            Else
                Throw New ApplicationException(String.Format("Zerodha command execution did not return anything, command:{0}", execCommand.ToString))
            End If
            Return ret
        End Function
#End Region

#Region "Cancel Order"
        Public Overrides Async Function CancelBOOrderAsync(ByVal orderId As String, ByVal parentOrderID As String) As Task(Of Dictionary(Of String, Object))
            'logger.Debug("ModifyStoplossOrderAsync, parameters:{0},{1}", orderId, parentOrderID)
            Dim ret As Dictionary(Of String, Object) = Nothing
            Dim execCommand As ExecutionCommands = ExecutionCommands.CancelOrder
            _cts.Token.ThrowIfCancellationRequested()
            Dim tradeParameters As New Dictionary(Of String, Object) From {
                {"OrderId", orderId},
                {"ParentOrderId", parentOrderID},
                {"Variety", Constants.VARIETY_BO}
            }
            Dim tempAllRet As Dictionary(Of String, Object) = Nothing
            Try
                tempAllRet = Await ExecuteCommandAsync(execCommand, tradeParameters).ConfigureAwait(False)
            Catch tex As TokenException
                Throw New ZerodhaBusinessException(tex.Message, tex, AdapterBusinessException.TypeOfException.TokenException)
            Catch gex As GeneralException
                Throw New ZerodhaBusinessException(gex.Message, gex, AdapterBusinessException.TypeOfException.GeneralException)
            Catch pex As PermissionException
                Throw New ZerodhaBusinessException(pex.Message, pex, AdapterBusinessException.TypeOfException.PermissionException)
            Catch oex As OrderException
                Throw New ZerodhaBusinessException(oex.Message, oex, AdapterBusinessException.TypeOfException.OrderException)
            Catch iex As InputException
                Throw New ZerodhaBusinessException(iex.Message, iex, AdapterBusinessException.TypeOfException.InputException)
            Catch dex As DataException
                Throw New ZerodhaBusinessException(dex.Message, dex, AdapterBusinessException.TypeOfException.DataException)
            Catch nex As NetworkException
                Throw New ZerodhaBusinessException(nex.Message, nex, AdapterBusinessException.TypeOfException.NetworkException)
            Catch ex As Exception
                Throw ex
            End Try
            _cts.Token.ThrowIfCancellationRequested()

            Dim tempRet As Object = Nothing
            If tempAllRet IsNot Nothing AndAlso tempAllRet.ContainsKey(execCommand.ToString) Then
                tempRet = tempAllRet(execCommand.ToString)
                If tempRet IsNot Nothing Then
                    Dim errorMessage As String = ParentController.GetErrorResponse(tempRet)
                    If errorMessage IsNot Nothing Then
                        Throw New ApplicationException(errorMessage)
                    End If
                Else
                    Throw New ApplicationException(String.Format("Zerodha command execution did not return anything, command:{0}", execCommand.ToString))
                End If
            Else
                Throw New ApplicationException(String.Format("Relevant command was fired but not detected in the response, command:{0}", execCommand.ToString))
            End If

            If tempRet.GetType = GetType(Dictionary(Of String, Object)) Then
                OnHeartbeat(String.Format("Cancel Order successful, details:{0}", Utils.JsonSerialize(tempRet)))
                ret = CType(tempRet, Dictionary(Of String, Object))
            Else
                Throw New ApplicationException(String.Format("Zerodha command execution did not return anything, command:{0}", execCommand.ToString))
            End If
            Return ret
        End Function
        Public Overrides Async Function CancelCOOrderAsync(ByVal orderId As String, ByVal parentOrderID As String) As Task(Of Dictionary(Of String, Object))
            'logger.Debug("ModifyStoplossOrderAsync, parameters:{0},{1}", orderId, parentOrderID)
            Dim ret As Dictionary(Of String, Object) = Nothing
            Dim execCommand As ExecutionCommands = ExecutionCommands.CancelOrder
            _cts.Token.ThrowIfCancellationRequested()
            Dim tradeParameters As New Dictionary(Of String, Object) From {
                {"OrderId", orderId},
                {"ParentOrderId", parentOrderID},
                {"Variety", Constants.VARIETY_CO}
            }
            Dim tempAllRet As Dictionary(Of String, Object) = Nothing
            Try
                tempAllRet = Await ExecuteCommandAsync(execCommand, tradeParameters).ConfigureAwait(False)
            Catch tex As TokenException
                Throw New ZerodhaBusinessException(tex.Message, tex, AdapterBusinessException.TypeOfException.TokenException)
            Catch gex As GeneralException
                Throw New ZerodhaBusinessException(gex.Message, gex, AdapterBusinessException.TypeOfException.GeneralException)
            Catch pex As PermissionException
                Throw New ZerodhaBusinessException(pex.Message, pex, AdapterBusinessException.TypeOfException.PermissionException)
            Catch oex As OrderException
                Throw New ZerodhaBusinessException(oex.Message, oex, AdapterBusinessException.TypeOfException.OrderException)
            Catch iex As InputException
                Throw New ZerodhaBusinessException(iex.Message, iex, AdapterBusinessException.TypeOfException.InputException)
            Catch dex As DataException
                Throw New ZerodhaBusinessException(dex.Message, dex, AdapterBusinessException.TypeOfException.DataException)
            Catch nex As NetworkException
                Throw New ZerodhaBusinessException(nex.Message, nex, AdapterBusinessException.TypeOfException.NetworkException)
            Catch ex As Exception
                Throw ex
            End Try
            _cts.Token.ThrowIfCancellationRequested()

            Dim tempRet As Object = Nothing
            If tempAllRet IsNot Nothing AndAlso tempAllRet.ContainsKey(execCommand.ToString) Then
                tempRet = tempAllRet(execCommand.ToString)
                If tempRet IsNot Nothing Then
                    Dim errorMessage As String = ParentController.GetErrorResponse(tempRet)
                    If errorMessage IsNot Nothing Then
                        Throw New ApplicationException(errorMessage)
                    End If
                Else
                    Throw New ApplicationException(String.Format("Zerodha command execution did not return anything, command:{0}", execCommand.ToString))
                End If
            Else
                Throw New ApplicationException(String.Format("Relevant command was fired but not detected in the response, command:{0}", execCommand.ToString))
            End If

            If tempRet.GetType = GetType(Dictionary(Of String, Object)) Then
                OnHeartbeat(String.Format("Cancel Order successful, details:{0}", Utils.JsonSerialize(tempRet)))
                ret = CType(tempRet, Dictionary(Of String, Object))
            Else
                Throw New ApplicationException(String.Format("Zerodha command execution did not return anything, command:{0}", execCommand.ToString))
            End If
            Return ret
        End Function
        Public Overrides Async Function CancelRegularOrderAsync(ByVal orderId As String, ByVal parentOrderID As String) As Task(Of Dictionary(Of String, Object))
            'logger.Debug("ModifyStoplossOrderAsync, parameters:{0},{1}", orderId, parentOrderID)
            Dim ret As Dictionary(Of String, Object) = Nothing
            Dim execCommand As ExecutionCommands = ExecutionCommands.CancelOrder
            _cts.Token.ThrowIfCancellationRequested()
            Dim tradeParameters As New Dictionary(Of String, Object) From {
                {"OrderId", orderId},
                {"ParentOrderId", parentOrderID},
                {"Variety", Constants.VARIETY_REGULAR}
            }
            Dim tempAllRet As Dictionary(Of String, Object) = Nothing
            Try
                tempAllRet = Await ExecuteCommandAsync(execCommand, tradeParameters).ConfigureAwait(False)
            Catch tex As TokenException
                Throw New ZerodhaBusinessException(tex.Message, tex, AdapterBusinessException.TypeOfException.TokenException)
            Catch gex As GeneralException
                Throw New ZerodhaBusinessException(gex.Message, gex, AdapterBusinessException.TypeOfException.GeneralException)
            Catch pex As PermissionException
                Throw New ZerodhaBusinessException(pex.Message, pex, AdapterBusinessException.TypeOfException.PermissionException)
            Catch oex As OrderException
                Throw New ZerodhaBusinessException(oex.Message, oex, AdapterBusinessException.TypeOfException.OrderException)
            Catch iex As InputException
                Throw New ZerodhaBusinessException(iex.Message, iex, AdapterBusinessException.TypeOfException.InputException)
            Catch dex As DataException
                Throw New ZerodhaBusinessException(dex.Message, dex, AdapterBusinessException.TypeOfException.DataException)
            Catch nex As NetworkException
                Throw New ZerodhaBusinessException(nex.Message, nex, AdapterBusinessException.TypeOfException.NetworkException)
            Catch ex As Exception
                Throw ex
            End Try
            _cts.Token.ThrowIfCancellationRequested()

            Dim tempRet As Object = Nothing
            If tempAllRet IsNot Nothing AndAlso tempAllRet.ContainsKey(execCommand.ToString) Then
                tempRet = tempAllRet(execCommand.ToString)
                If tempRet IsNot Nothing Then
                    Dim errorMessage As String = ParentController.GetErrorResponse(tempRet)
                    If errorMessage IsNot Nothing Then
                        Throw New ApplicationException(errorMessage)
                    End If
                Else
                    Throw New ApplicationException(String.Format("Zerodha command execution did not return anything, command:{0}", execCommand.ToString))
                End If
            Else
                Throw New ApplicationException(String.Format("Relevant command was fired but not detected in the response, command:{0}", execCommand.ToString))
            End If

            If tempRet.GetType = GetType(Dictionary(Of String, Object)) Then
                OnHeartbeat(String.Format("Cancel Order successful, details:{0}", Utils.JsonSerialize(tempRet)))
                ret = CType(tempRet, Dictionary(Of String, Object))
            Else
                Throw New ApplicationException(String.Format("Zerodha command execution did not return anything, command:{0}", execCommand.ToString))
            End If
            Return ret
        End Function
#End Region

#Region "Place BO"
        Public Overrides Async Function PlaceBOLimitMISOrderAsync(ByVal tradeExchange As String,
                                                                   ByVal tradingSymbol As String,
                                                                   ByVal transaction As IOrder.TypeOfTransaction,
                                                                   ByVal quantity As Integer,
                                                                   ByVal price As Decimal,
                                                                   ByVal squareOffValue As Decimal,
                                                                   ByVal stopLossValue As Decimal,
                                                                   ByVal tag As String) As Task(Of Dictionary(Of String, Object))
            Dim ret As Dictionary(Of String, Object) = Nothing
            Dim execCommand As ExecutionCommands = ExecutionCommands.PlaceOrder
            _cts.Token.ThrowIfCancellationRequested()

            Dim transactionDirection As String = Nothing
            Select Case transaction
                Case IOrder.TypeOfTransaction.Buy
                    transactionDirection = Constants.TRANSACTION_TYPE_BUY
                Case IOrder.TypeOfTransaction.Sell
                    transactionDirection = Constants.TRANSACTION_TYPE_SELL
            End Select
            Dim tradeParameters As New Dictionary(Of String, Object) From {
                {"Exchange", tradeExchange},
                {"TradingSymbol", tradingSymbol},
                {"TransactionType", transactionDirection},
                {"Quantity", quantity},
                {"Price", price},
                {"Product", Constants.PRODUCT_MIS},
                {"OrderType", Constants.ORDER_TYPE_LIMIT},
                {"Validity", Constants.VALIDITY_DAY},
                {"TriggerPrice", Nothing},
                {"SquareOffValue", squareOffValue},
                {"StoplossValue", stopLossValue},
                {"Variety", Constants.VARIETY_BO},
                {"Tag", tag}
            }
            Dim tempAllRet As Dictionary(Of String, Object) = Nothing
            Try
                tempAllRet = Await ExecuteCommandAsync(execCommand, tradeParameters).ConfigureAwait(False)
            Catch tex As TokenException
                Throw New ZerodhaBusinessException(tex.Message, tex, AdapterBusinessException.TypeOfException.TokenException)
            Catch gex As GeneralException
                Throw New ZerodhaBusinessException(gex.Message, gex, AdapterBusinessException.TypeOfException.GeneralException)
            Catch pex As PermissionException
                Throw New ZerodhaBusinessException(pex.Message, pex, AdapterBusinessException.TypeOfException.PermissionException)
            Catch oex As OrderException
                Throw New ZerodhaBusinessException(oex.Message, oex, AdapterBusinessException.TypeOfException.OrderException)
            Catch iex As InputException
                Throw New ZerodhaBusinessException(iex.Message, iex, AdapterBusinessException.TypeOfException.InputException)
            Catch dex As DataException
                Throw New ZerodhaBusinessException(dex.Message, dex, AdapterBusinessException.TypeOfException.DataException)
            Catch nex As NetworkException
                Throw New ZerodhaBusinessException(nex.Message, nex, AdapterBusinessException.TypeOfException.NetworkException)
            Catch ex As Exception
                Throw ex
            End Try
            _cts.Token.ThrowIfCancellationRequested()

            Dim tempRet As Object = Nothing
            If tempAllRet IsNot Nothing AndAlso tempAllRet.ContainsKey(execCommand.ToString) Then
                tempRet = tempAllRet(execCommand.ToString)
                If tempRet IsNot Nothing Then
                    Dim errorMessage As String = ParentController.GetErrorResponse(tempRet)
                    If errorMessage IsNot Nothing Then
                        Throw New ApplicationException(errorMessage)
                    End If
                Else
                    Throw New ApplicationException(String.Format("Zerodha command execution did not return anything, command:{0}", execCommand.ToString))
                End If
            Else
                Throw New ApplicationException(String.Format("Relevant command was fired but not detected in the response, command:{0}", execCommand.ToString))
            End If

            If tempRet.GetType = GetType(Dictionary(Of String, Object)) Then
                OnHeartbeat(String.Format("PlaceOrder successful, details:{0}", Utils.JsonSerialize(tempRet)))
                ret = CType(tempRet, Dictionary(Of String, Object))
            Else
                Throw New ApplicationException(String.Format("Zerodha command execution did not return anything, command:{0}", execCommand.ToString))
            End If
            Return ret
        End Function
        Public Overrides Async Function PlaceBOSLMISOrderAsync(ByVal tradeExchange As String,
                                                                ByVal tradingSymbol As String,
                                                                ByVal transaction As IOrder.TypeOfTransaction,
                                                                ByVal quantity As Integer,
                                                                ByVal price As Decimal,
                                                                ByVal triggerPrice As Decimal,
                                                                ByVal squareOffValue As Decimal,
                                                                ByVal stopLossValue As Decimal,
                                                                ByVal tag As String) As Task(Of Dictionary(Of String, Object))
            Dim ret As Dictionary(Of String, Object) = Nothing
            Dim execCommand As ExecutionCommands = ExecutionCommands.PlaceOrder
            _cts.Token.ThrowIfCancellationRequested()

            Dim transactionDirection As String = Nothing
            Select Case transaction
                Case IOrder.TypeOfTransaction.Buy
                    transactionDirection = Constants.TRANSACTION_TYPE_BUY
                Case IOrder.TypeOfTransaction.Sell
                    transactionDirection = Constants.TRANSACTION_TYPE_SELL
            End Select
            Dim tradeParameters As New Dictionary(Of String, Object) From {
                {"Exchange", tradeExchange},
                {"TradingSymbol", tradingSymbol},
                {"TransactionType", transactionDirection},
                {"Quantity", quantity},
                {"Price", price},
                {"Product", Constants.PRODUCT_MIS},
                {"OrderType", Constants.ORDER_TYPE_SL},
                {"Validity", Constants.VALIDITY_DAY},
                {"TriggerPrice", triggerPrice},
                {"SquareOffValue", squareOffValue},
                {"StoplossValue", stopLossValue},
                {"Variety", Constants.VARIETY_BO},
                {"Tag", tag}
            }
            Dim tempAllRet As Dictionary(Of String, Object) = Nothing
            Try
                tempAllRet = Await ExecuteCommandAsync(execCommand, tradeParameters).ConfigureAwait(False)
            Catch tex As TokenException
                Throw New ZerodhaBusinessException(tex.Message, tex, AdapterBusinessException.TypeOfException.TokenException)
            Catch gex As GeneralException
                Throw New ZerodhaBusinessException(gex.Message, gex, AdapterBusinessException.TypeOfException.GeneralException)
            Catch pex As PermissionException
                Throw New ZerodhaBusinessException(pex.Message, pex, AdapterBusinessException.TypeOfException.PermissionException)
            Catch oex As OrderException
                Throw New ZerodhaBusinessException(oex.Message, oex, AdapterBusinessException.TypeOfException.OrderException)
            Catch iex As InputException
                Throw New ZerodhaBusinessException(iex.Message, iex, AdapterBusinessException.TypeOfException.InputException)
            Catch dex As DataException
                Throw New ZerodhaBusinessException(dex.Message, dex, AdapterBusinessException.TypeOfException.DataException)
            Catch nex As NetworkException
                Throw New ZerodhaBusinessException(nex.Message, nex, AdapterBusinessException.TypeOfException.NetworkException)
            Catch ex As Exception
                Throw ex
            End Try
            _cts.Token.ThrowIfCancellationRequested()

            Dim tempRet As Object = Nothing
            If tempAllRet IsNot Nothing AndAlso tempAllRet.ContainsKey(execCommand.ToString) Then
                tempRet = tempAllRet(execCommand.ToString)
                If tempRet IsNot Nothing Then
                    Dim errorMessage As String = ParentController.GetErrorResponse(tempRet)
                    If errorMessage IsNot Nothing Then
                        Throw New ApplicationException(errorMessage)
                    End If
                Else
                    Throw New ApplicationException(String.Format("Zerodha command execution did not return anything, command:{0}", execCommand.ToString))
                End If
            Else
                Throw New ApplicationException(String.Format("Relevant command was fired but not detected in the response, command:{0}", execCommand.ToString))
            End If

            If tempRet.GetType = GetType(Dictionary(Of String, Object)) Then
                OnHeartbeat(String.Format("PlaceOrder successful, details:{0}", Utils.JsonSerialize(tempRet)))
                ret = CType(tempRet, Dictionary(Of String, Object))
            Else
                Throw New ApplicationException(String.Format("Zerodha command execution did not return anything, command:{0}", execCommand.ToString))
            End If
            Return ret
        End Function
#End Region

#Region "Place CO"
        Public Overrides Async Function PlaceCOMarketMISOrderAsync(ByVal tradeExchange As String,
                                                                   ByVal tradingSymbol As String,
                                                                   ByVal transaction As IOrder.TypeOfTransaction,
                                                                   ByVal quantity As Integer,
                                                                   ByVal triggerPrice As Decimal,
                                                                   ByVal tag As String) As Task(Of Dictionary(Of String, Object))
            Dim ret As Dictionary(Of String, Object) = Nothing
            Dim execCommand As ExecutionCommands = ExecutionCommands.PlaceOrder
            _cts.Token.ThrowIfCancellationRequested()

            Dim transactionDirection As String = Nothing
            Select Case transaction
                Case IOrder.TypeOfTransaction.Buy
                    transactionDirection = Constants.TRANSACTION_TYPE_BUY
                Case IOrder.TypeOfTransaction.Sell
                    transactionDirection = Constants.TRANSACTION_TYPE_SELL
            End Select
            Dim tradeParameters As New Dictionary(Of String, Object) From {
                {"Exchange", tradeExchange},
                {"TradingSymbol", tradingSymbol},
                {"TransactionType", transactionDirection},
                {"Quantity", quantity},
                {"Price", Nothing},
                {"Product", Constants.PRODUCT_MIS},
                {"OrderType", Constants.ORDER_TYPE_MARKET},
                {"Validity", Constants.VALIDITY_DAY},
                {"TriggerPrice", triggerPrice},
                {"SquareOffValue", Nothing},
                {"StoplossValue", Nothing},
                {"Variety", Constants.VARIETY_CO},
                {"Tag", tag}
            }
            Dim tempAllRet As Dictionary(Of String, Object) = Nothing
            Try
                tempAllRet = Await ExecuteCommandAsync(execCommand, tradeParameters).ConfigureAwait(False)
            Catch tex As TokenException
                Throw New ZerodhaBusinessException(tex.Message, tex, AdapterBusinessException.TypeOfException.TokenException)
            Catch gex As GeneralException
                Throw New ZerodhaBusinessException(gex.Message, gex, AdapterBusinessException.TypeOfException.GeneralException)
            Catch pex As PermissionException
                Throw New ZerodhaBusinessException(pex.Message, pex, AdapterBusinessException.TypeOfException.PermissionException)
            Catch oex As OrderException
                Throw New ZerodhaBusinessException(oex.Message, oex, AdapterBusinessException.TypeOfException.OrderException)
            Catch iex As InputException
                Throw New ZerodhaBusinessException(iex.Message, iex, AdapterBusinessException.TypeOfException.InputException)
            Catch dex As DataException
                Throw New ZerodhaBusinessException(dex.Message, dex, AdapterBusinessException.TypeOfException.DataException)
            Catch nex As NetworkException
                Throw New ZerodhaBusinessException(nex.Message, nex, AdapterBusinessException.TypeOfException.NetworkException)
            Catch ex As Exception
                Throw ex
            End Try
            _cts.Token.ThrowIfCancellationRequested()

            Dim tempRet As Object = Nothing
            If tempAllRet IsNot Nothing AndAlso tempAllRet.ContainsKey(execCommand.ToString) Then
                tempRet = tempAllRet(execCommand.ToString)
                If tempRet IsNot Nothing Then
                    Dim errorMessage As String = ParentController.GetErrorResponse(tempRet)
                    If errorMessage IsNot Nothing Then
                        Throw New ApplicationException(errorMessage)
                    End If
                Else
                    Throw New ApplicationException(String.Format("Zerodha command execution did not return anything, command:{0}", execCommand.ToString))
                End If
            Else
                Throw New ApplicationException(String.Format("Relevant command was fired but not detected in the response, command:{0}", execCommand.ToString))
            End If

            If tempRet.GetType = GetType(Dictionary(Of String, Object)) Then
                OnHeartbeat(String.Format("PlaceOrder successful, details:{0}", Utils.JsonSerialize(tempRet)))
                ret = CType(tempRet, Dictionary(Of String, Object))
            Else
                Throw New ApplicationException(String.Format("Zerodha command execution did not return anything, command:{0}", execCommand.ToString))
            End If
            Return ret
        End Function
#End Region

#Region "Place Regular MIS"
        Public Overrides Async Function PlaceRegularMarketMISOrderAsync(ByVal tradeExchange As String,
                                                                     ByVal tradingSymbol As String,
                                                                     ByVal transaction As IOrder.TypeOfTransaction,
                                                                     ByVal quantity As Integer,
                                                                     ByVal tag As String) As Task(Of Dictionary(Of String, Object))
            Dim ret As Dictionary(Of String, Object) = Nothing
            Dim execCommand As ExecutionCommands = ExecutionCommands.PlaceOrder
            _cts.Token.ThrowIfCancellationRequested()

            Dim transactionDirection As String = Nothing
            Select Case transaction
                Case IOrder.TypeOfTransaction.Buy
                    transactionDirection = Constants.TRANSACTION_TYPE_BUY
                Case IOrder.TypeOfTransaction.Sell
                    transactionDirection = Constants.TRANSACTION_TYPE_SELL
            End Select
            Dim tradeParameters As New Dictionary(Of String, Object) From {
                {"Exchange", tradeExchange},
                {"TradingSymbol", tradingSymbol},
                {"TransactionType", transactionDirection},
                {"Quantity", quantity},
                {"Price", Nothing},
                {"Product", Constants.PRODUCT_MIS},
                {"OrderType", Constants.ORDER_TYPE_MARKET},
                {"Validity", Constants.VALIDITY_DAY},
                {"TriggerPrice", Nothing},
                {"SquareOffValue", Nothing},
                {"StoplossValue", Nothing},
                {"Variety", Constants.VARIETY_REGULAR},
                {"Tag", tag}
            }
            Dim tempAllRet As Dictionary(Of String, Object) = Nothing
            Try
                tempAllRet = Await ExecuteCommandAsync(execCommand, tradeParameters).ConfigureAwait(False)
            Catch tex As TokenException
                Throw New ZerodhaBusinessException(tex.Message, tex, AdapterBusinessException.TypeOfException.TokenException)
            Catch gex As GeneralException
                Throw New ZerodhaBusinessException(gex.Message, gex, AdapterBusinessException.TypeOfException.GeneralException)
            Catch pex As PermissionException
                Throw New ZerodhaBusinessException(pex.Message, pex, AdapterBusinessException.TypeOfException.PermissionException)
            Catch oex As OrderException
                Throw New ZerodhaBusinessException(oex.Message, oex, AdapterBusinessException.TypeOfException.OrderException)
            Catch iex As InputException
                Throw New ZerodhaBusinessException(iex.Message, iex, AdapterBusinessException.TypeOfException.InputException)
            Catch dex As DataException
                Throw New ZerodhaBusinessException(dex.Message, dex, AdapterBusinessException.TypeOfException.DataException)
            Catch nex As NetworkException
                Throw New ZerodhaBusinessException(nex.Message, nex, AdapterBusinessException.TypeOfException.NetworkException)
            Catch ex As Exception
                Throw ex
            End Try
            _cts.Token.ThrowIfCancellationRequested()

            Dim tempRet As Object = Nothing
            If tempAllRet IsNot Nothing AndAlso tempAllRet.ContainsKey(execCommand.ToString) Then
                tempRet = tempAllRet(execCommand.ToString)
                If tempRet IsNot Nothing Then
                    Dim errorMessage As String = ParentController.GetErrorResponse(tempRet)
                    If errorMessage IsNot Nothing Then
                        Throw New ApplicationException(errorMessage)
                    End If
                Else
                    Throw New ApplicationException(String.Format("Zerodha command execution did not return anything, command:{0}", execCommand.ToString))
                End If
            Else
                Throw New ApplicationException(String.Format("Relevant command was fired but not detected in the response, command:{0}", execCommand.ToString))
            End If

            If tempRet.GetType = GetType(Dictionary(Of String, Object)) Then
                OnHeartbeat(String.Format("PlaceOrder successful, details:{0}", Utils.JsonSerialize(tempRet)))
                ret = CType(tempRet, Dictionary(Of String, Object))
            Else
                Throw New ApplicationException(String.Format("Zerodha command execution did not return anything, command:{0}", execCommand.ToString))
            End If
            Return ret
        End Function
        Public Overrides Async Function PlaceRegularLimitMISOrderAsync(ByVal tradeExchange As String,
                                                                    ByVal tradingSymbol As String,
                                                                    ByVal transaction As IOrder.TypeOfTransaction,
                                                                    ByVal quantity As Integer,
                                                                    ByVal price As Decimal,
                                                                    ByVal tag As String) As Task(Of Dictionary(Of String, Object))
            Dim ret As Dictionary(Of String, Object) = Nothing
            Dim execCommand As ExecutionCommands = ExecutionCommands.PlaceOrder
            _cts.Token.ThrowIfCancellationRequested()

            Dim transactionDirection As String = Nothing
            Select Case transaction
                Case IOrder.TypeOfTransaction.Buy
                    transactionDirection = Constants.TRANSACTION_TYPE_BUY
                Case IOrder.TypeOfTransaction.Sell
                    transactionDirection = Constants.TRANSACTION_TYPE_SELL
            End Select
            Dim tradeParameters As New Dictionary(Of String, Object) From {
                {"Exchange", tradeExchange},
                {"TradingSymbol", tradingSymbol},
                {"TransactionType", transactionDirection},
                {"Quantity", quantity},
                {"Price", price},
                {"Product", Constants.PRODUCT_MIS},
                {"OrderType", Constants.ORDER_TYPE_LIMIT},
                {"Validity", Constants.VALIDITY_DAY},
                {"TriggerPrice", Nothing},
                {"SquareOffValue", Nothing},
                {"StoplossValue", Nothing},
                {"Variety", Constants.VARIETY_REGULAR},
                {"Tag", tag}
            }
            Dim tempAllRet As Dictionary(Of String, Object) = Nothing
            Try
                tempAllRet = Await ExecuteCommandAsync(execCommand, tradeParameters).ConfigureAwait(False)
            Catch tex As TokenException
                Throw New ZerodhaBusinessException(tex.Message, tex, AdapterBusinessException.TypeOfException.TokenException)
            Catch gex As GeneralException
                Throw New ZerodhaBusinessException(gex.Message, gex, AdapterBusinessException.TypeOfException.GeneralException)
            Catch pex As PermissionException
                Throw New ZerodhaBusinessException(pex.Message, pex, AdapterBusinessException.TypeOfException.PermissionException)
            Catch oex As OrderException
                Throw New ZerodhaBusinessException(oex.Message, oex, AdapterBusinessException.TypeOfException.OrderException)
            Catch iex As InputException
                Throw New ZerodhaBusinessException(iex.Message, iex, AdapterBusinessException.TypeOfException.InputException)
            Catch dex As DataException
                Throw New ZerodhaBusinessException(dex.Message, dex, AdapterBusinessException.TypeOfException.DataException)
            Catch nex As NetworkException
                Throw New ZerodhaBusinessException(nex.Message, nex, AdapterBusinessException.TypeOfException.NetworkException)
            Catch ex As Exception
                Throw ex
            End Try
            _cts.Token.ThrowIfCancellationRequested()

            Dim tempRet As Object = Nothing
            If tempAllRet IsNot Nothing AndAlso tempAllRet.ContainsKey(execCommand.ToString) Then
                tempRet = tempAllRet(execCommand.ToString)
                If tempRet IsNot Nothing Then
                    Dim errorMessage As String = ParentController.GetErrorResponse(tempRet)
                    If errorMessage IsNot Nothing Then
                        Throw New ApplicationException(errorMessage)
                    End If
                Else
                    Throw New ApplicationException(String.Format("Zerodha command execution did not return anything, command:{0}", execCommand.ToString))
                End If
            Else
                Throw New ApplicationException(String.Format("Relevant command was fired but not detected in the response, command:{0}", execCommand.ToString))
            End If

            If tempRet.GetType = GetType(Dictionary(Of String, Object)) Then
                OnHeartbeat(String.Format("PlaceOrder successful, details:{0}", Utils.JsonSerialize(tempRet)))
                ret = CType(tempRet, Dictionary(Of String, Object))
            Else
                Throw New ApplicationException(String.Format("Zerodha command execution did not return anything, command:{0}", execCommand.ToString))
            End If
            Return ret
        End Function
        Public Overrides Async Function PlaceRegularSLMMISOrderAsync(ByVal tradeExchange As String,
                                                                  ByVal tradingSymbol As String,
                                                                  ByVal transaction As IOrder.TypeOfTransaction,
                                                                  ByVal quantity As Integer,
                                                                  ByVal triggerPrice As Decimal,
                                                                  ByVal tag As String) As Task(Of Dictionary(Of String, Object))
            Dim ret As Dictionary(Of String, Object) = Nothing
            Dim execCommand As ExecutionCommands = ExecutionCommands.PlaceOrder
            _cts.Token.ThrowIfCancellationRequested()

            Dim transactionDirection As String = Nothing
            Select Case transaction
                Case IOrder.TypeOfTransaction.Buy
                    transactionDirection = Constants.TRANSACTION_TYPE_BUY
                Case IOrder.TypeOfTransaction.Sell
                    transactionDirection = Constants.TRANSACTION_TYPE_SELL
            End Select
            Dim tradeParameters As New Dictionary(Of String, Object) From {
                {"Exchange", tradeExchange},
                {"TradingSymbol", tradingSymbol},
                {"TransactionType", transactionDirection},
                {"Quantity", quantity},
                {"Price", Nothing},
                {"Product", Constants.PRODUCT_MIS},
                {"OrderType", Constants.ORDER_TYPE_SLM},
                {"Validity", Constants.VALIDITY_DAY},
                {"TriggerPrice", triggerPrice},
                {"SquareOffValue", Nothing},
                {"StoplossValue", Nothing},
                {"Variety", Constants.VARIETY_REGULAR},
                {"Tag", tag}
            }
            Dim tempAllRet As Dictionary(Of String, Object) = Nothing
            Try
                tempAllRet = Await ExecuteCommandAsync(execCommand, tradeParameters).ConfigureAwait(False)
            Catch tex As TokenException
                Throw New ZerodhaBusinessException(tex.Message, tex, AdapterBusinessException.TypeOfException.TokenException)
            Catch gex As GeneralException
                Throw New ZerodhaBusinessException(gex.Message, gex, AdapterBusinessException.TypeOfException.GeneralException)
            Catch pex As PermissionException
                Throw New ZerodhaBusinessException(pex.Message, pex, AdapterBusinessException.TypeOfException.PermissionException)
            Catch oex As OrderException
                Throw New ZerodhaBusinessException(oex.Message, oex, AdapterBusinessException.TypeOfException.OrderException)
            Catch iex As InputException
                Throw New ZerodhaBusinessException(iex.Message, iex, AdapterBusinessException.TypeOfException.InputException)
            Catch dex As DataException
                Throw New ZerodhaBusinessException(dex.Message, dex, AdapterBusinessException.TypeOfException.DataException)
            Catch nex As NetworkException
                Throw New ZerodhaBusinessException(nex.Message, nex, AdapterBusinessException.TypeOfException.NetworkException)
            Catch ex As Exception
                Throw ex
            End Try
            _cts.Token.ThrowIfCancellationRequested()

            Dim tempRet As Object = Nothing
            If tempAllRet IsNot Nothing AndAlso tempAllRet.ContainsKey(execCommand.ToString) Then
                tempRet = tempAllRet(execCommand.ToString)
                If tempRet IsNot Nothing Then
                    Dim errorMessage As String = ParentController.GetErrorResponse(tempRet)
                    If errorMessage IsNot Nothing Then
                        Throw New ApplicationException(errorMessage)
                    End If
                Else
                    Throw New ApplicationException(String.Format("Zerodha command execution did not return anything, command:{0}", execCommand.ToString))
                End If
            Else
                Throw New ApplicationException(String.Format("Relevant command was fired but not detected in the response, command:{0}", execCommand.ToString))
            End If

            If tempRet.GetType = GetType(Dictionary(Of String, Object)) Then
                OnHeartbeat(String.Format("PlaceOrder successful, details:{0}", Utils.JsonSerialize(tempRet)))
                ret = CType(tempRet, Dictionary(Of String, Object))
            Else
                Throw New ApplicationException(String.Format("Zerodha command execution did not return anything, command:{0}", execCommand.ToString))
            End If
            Return ret
        End Function
#End Region

#Region "Place Regular CNC"
        Public Overrides Async Function PlaceRegularMarketCNCOrderAsync(ByVal tradeExchange As String,
                                                                         ByVal tradingSymbol As String,
                                                                         ByVal transaction As IOrder.TypeOfTransaction,
                                                                         ByVal quantity As Integer,
                                                                         ByVal tag As String) As Task(Of Dictionary(Of String, Object))
            Dim ret As Dictionary(Of String, Object) = Nothing
            Dim execCommand As ExecutionCommands = ExecutionCommands.PlaceOrder
            _cts.Token.ThrowIfCancellationRequested()

            Dim transactionDirection As String = Nothing
            Select Case transaction
                Case IOrder.TypeOfTransaction.Buy
                    transactionDirection = Constants.TRANSACTION_TYPE_BUY
                Case IOrder.TypeOfTransaction.Sell
                    transactionDirection = Constants.TRANSACTION_TYPE_SELL
            End Select
            Dim tradeParameters As New Dictionary(Of String, Object) From {
                {"Exchange", tradeExchange},
                {"TradingSymbol", tradingSymbol},
                {"TransactionType", transactionDirection},
                {"Quantity", quantity},
                {"Price", Nothing},
                {"Product", Constants.PRODUCT_CNC},
                {"OrderType", Constants.ORDER_TYPE_MARKET},
                {"Validity", Constants.VALIDITY_DAY},
                {"TriggerPrice", Nothing},
                {"SquareOffValue", Nothing},
                {"StoplossValue", Nothing},
                {"Variety", Constants.VARIETY_REGULAR},
                {"Tag", tag}
            }
            Dim tempAllRet As Dictionary(Of String, Object) = Nothing
            Try
                tempAllRet = Await ExecuteCommandAsync(execCommand, tradeParameters).ConfigureAwait(False)
            Catch tex As TokenException
                Throw New ZerodhaBusinessException(tex.Message, tex, AdapterBusinessException.TypeOfException.TokenException)
            Catch gex As GeneralException
                Throw New ZerodhaBusinessException(gex.Message, gex, AdapterBusinessException.TypeOfException.GeneralException)
            Catch pex As PermissionException
                Throw New ZerodhaBusinessException(pex.Message, pex, AdapterBusinessException.TypeOfException.PermissionException)
            Catch oex As OrderException
                Throw New ZerodhaBusinessException(oex.Message, oex, AdapterBusinessException.TypeOfException.OrderException)
            Catch iex As InputException
                Throw New ZerodhaBusinessException(iex.Message, iex, AdapterBusinessException.TypeOfException.InputException)
            Catch dex As DataException
                Throw New ZerodhaBusinessException(dex.Message, dex, AdapterBusinessException.TypeOfException.DataException)
            Catch nex As NetworkException
                Throw New ZerodhaBusinessException(nex.Message, nex, AdapterBusinessException.TypeOfException.NetworkException)
            Catch ex As Exception
                Throw ex
            End Try
            _cts.Token.ThrowIfCancellationRequested()

            Dim tempRet As Object = Nothing
            If tempAllRet IsNot Nothing AndAlso tempAllRet.ContainsKey(execCommand.ToString) Then
                tempRet = tempAllRet(execCommand.ToString)
                If tempRet IsNot Nothing Then
                    Dim errorMessage As String = ParentController.GetErrorResponse(tempRet)
                    If errorMessage IsNot Nothing Then
                        Throw New ApplicationException(errorMessage)
                    End If
                Else
                    Throw New ApplicationException(String.Format("Zerodha command execution did not return anything, command:{0}", execCommand.ToString))
                End If
            Else
                Throw New ApplicationException(String.Format("Relevant command was fired but not detected in the response, command:{0}", execCommand.ToString))
            End If

            If tempRet.GetType = GetType(Dictionary(Of String, Object)) Then
                OnHeartbeat(String.Format("PlaceOrder successful, details:{0}", Utils.JsonSerialize(tempRet)))
                ret = CType(tempRet, Dictionary(Of String, Object))
            Else
                Throw New ApplicationException(String.Format("Zerodha command execution did not return anything, command:{0}", execCommand.ToString))
            End If
            Return ret
        End Function
#End Region

#Region "Zerodha Commands"
        Private Async Function ExecuteCommandAsync(ByVal command As ExecutionCommands, ByVal stockData As Dictionary(Of String, Object)) As Task(Of Dictionary(Of String, Object))
            If command <> ExecutionCommands.GetOrders AndAlso command <> ExecutionCommands.GetQuotes Then
                logger.Debug("ExecuteCommandAsync, command:{0}, stockData:{1}", command.ToString, Utils.JsonSerialize(stockData))
            End If
            _cts.Token.ThrowIfCancellationRequested()
            Dim ret As Dictionary(Of String, Object) = Nothing

            Dim lastException As Exception = Nothing
            If command <> ExecutionCommands.GetOrders Then
                logger.Debug(String.Format("Firing Zerodha command to complete desired action, command:{0}", command.ToString))
            End If
            Select Case command
                Case ExecutionCommands.GetQuotes
                    Dim getQuotesResponse As Dictionary(Of String, Quote) = Nothing
                    If stockData IsNot Nothing AndAlso stockData.ContainsKey("instruments") Then
                        Dim index As Integer = -1
                        Dim subscriptionList() As String = Nothing
                        For Each runningInstruments As IInstrument In stockData("instruments")
                            _cts.Token.ThrowIfCancellationRequested()
                            index += 1
                            If index = 0 Then
                                ReDim subscriptionList(0)
                            Else
                                ReDim Preserve subscriptionList(UBound(subscriptionList) + 1)
                            End If
                            subscriptionList(index) = runningInstruments.InstrumentIdentifier
                        Next
                        If subscriptionList IsNot Nothing AndAlso subscriptionList.Length > 0 Then

                            getQuotesResponse = Await Task.Factory.StartNew(Function()
                                                                                Try
                                                                                    Return _Kite.GetQuote(subscriptionList)
                                                                                Catch ex As Exception
                                                                                    logger.Error(ex)
                                                                                    lastException = ex
                                                                                    Return Nothing
                                                                                End Try
                                                                            End Function).ConfigureAwait(False)
                            _cts.Token.ThrowIfCancellationRequested()
                            ret = New Dictionary(Of String, Object) From {{command.ToString, getQuotesResponse}}
                        End If
                    End If
                Case ExecutionCommands.GetPositions
                    Dim positions As PositionResponse = Nothing
                    positions = Await Task.Factory.StartNew(Function()
                                                                Try
                                                                    Return _Kite.GetPositions()
                                                                Catch ex As Exception
                                                                    logger.Error(ex)
                                                                    lastException = ex
                                                                    Return Nothing
                                                                End Try
                                                            End Function).ConfigureAwait(False)
                    _cts.Token.ThrowIfCancellationRequested()
                    ret = New Dictionary(Of String, Object) From {{command.ToString, positions}}
                Case ExecutionCommands.PlaceOrder
                    Dim placedOrders As Dictionary(Of String, Object) = Nothing
                    If stockData IsNot Nothing AndAlso stockData.Count > 0 Then
                        placedOrders = Await Task.Factory.StartNew(Function()
                                                                       Try
                                                                           Return _Kite.PlaceOrder(Exchange:=CType(stockData("Exchange"), String),
                                                                                                    TradingSymbol:=CType(stockData("TradingSymbol"), String),
                                                                                                    TransactionType:=CType(stockData("TransactionType"), String),
                                                                                                    Quantity:=CType(stockData("Quantity"), Integer),
                                                                                                    Price:=CType(stockData("Price"), Decimal),
                                                                                                    Product:=CType(stockData("Product"), String),
                                                                                                    OrderType:=CType(stockData("OrderType"), String),
                                                                                                    Validity:=CType(stockData("Validity"), String),
                                                                                                    TriggerPrice:=CType(stockData("TriggerPrice"), String),
                                                                                                    SquareOffValue:=CType(stockData("SquareOffValue"), Decimal),
                                                                                                    StoplossValue:=CType(stockData("StoplossValue"), Decimal),
                                                                                                    Variety:=CType(stockData("Variety"), String),
                                                                                                    Tag:=CType(stockData("Tag"), String))
                                                                       Catch ex As Exception
                                                                           logger.Error(ex)
                                                                           lastException = ex
                                                                           Return Nothing
                                                                       End Try
                                                                   End Function).ConfigureAwait(False)
                        _cts.Token.ThrowIfCancellationRequested()
                    End If
                    ret = New Dictionary(Of String, Object) From {{command.ToString, placedOrders}}
                Case ExecutionCommands.ModifyOrderQuantity
                    Dim modifiedOrdersQuantity As Dictionary(Of String, Object) = Nothing
                    If stockData IsNot Nothing AndAlso stockData.Count > 0 Then
                        modifiedOrdersQuantity = Await Task.Factory.StartNew(Function()
                                                                                 Try
                                                                                     Return _Kite.ModifyOrder(OrderId:=CType(stockData("OrderId"), String),
                                                                                                              Quantity:=CType(stockData("Quantity"), String))
                                                                                 Catch ex As Exception
                                                                                     logger.Error(ex)
                                                                                     lastException = ex
                                                                                     Return Nothing
                                                                                 End Try
                                                                             End Function).ConfigureAwait(False)
                        _cts.Token.ThrowIfCancellationRequested()
                    End If
                    ret = New Dictionary(Of String, Object) From {{command.ToString, modifiedOrdersQuantity}}
                Case ExecutionCommands.ModifyTargetOrderPrice, ExecutionCommands.ModifyOrderPrice
                    Dim modifiedOrdersPrice As Dictionary(Of String, Object) = Nothing
                    If stockData IsNot Nothing AndAlso stockData.Count > 0 Then
                        modifiedOrdersPrice = Await Task.Factory.StartNew(Function()
                                                                              Try
                                                                                  Return _Kite.ModifyOrder(OrderId:=CType(stockData("OrderId"), String),
                                                                                                           Price:=CType(stockData("Price"), Decimal))
                                                                              Catch ex As Exception
                                                                                  logger.Error(ex)
                                                                                  lastException = ex
                                                                                  Return Nothing
                                                                              End Try
                                                                          End Function).ConfigureAwait(False)
                        _cts.Token.ThrowIfCancellationRequested()
                    End If
                    ret = New Dictionary(Of String, Object) From {{command.ToString, modifiedOrdersPrice}}
                Case ExecutionCommands.ModifySLOrderPrice
                    Dim modifiedOrdersPrice As Dictionary(Of String, Object) = Nothing
                    If stockData IsNot Nothing AndAlso stockData.Count > 0 Then
                        modifiedOrdersPrice = Await Task.Factory.StartNew(Function()
                                                                              Try
                                                                                  Return _Kite.ModifyOrder(OrderId:=CType(stockData("OrderId"), String),
                                                                                                           TriggerPrice:=CType(stockData("TriggerPrice"), Decimal))
                                                                              Catch ex As Exception
                                                                                  logger.Error(ex)
                                                                                  lastException = ex
                                                                                  Return Nothing
                                                                              End Try
                                                                          End Function).ConfigureAwait(False)
                        _cts.Token.ThrowIfCancellationRequested()
                    End If
                    ret = New Dictionary(Of String, Object) From {{command.ToString, modifiedOrdersPrice}}
                Case ExecutionCommands.CancelOrder
                    Dim cancelledOrder As Dictionary(Of String, Object) = Nothing
                    If stockData IsNot Nothing AndAlso stockData.Count > 0 Then
                        cancelledOrder = Await Task.Factory.StartNew(Function()
                                                                         Try
                                                                             Return _Kite.CancelOrder(OrderId:=CType(stockData("OrderId"), String),
                                                                                                       ParentOrderId:=CType(stockData("ParentOrderId"), String),
                                                                                                       Variety:=CType(stockData("Variety"), String))
                                                                         Catch ex As Exception
                                                                             logger.Error(ex)
                                                                             lastException = ex
                                                                             Return Nothing
                                                                         End Try
                                                                     End Function).ConfigureAwait(False)
                        _cts.Token.ThrowIfCancellationRequested()
                    End If
                    ret = New Dictionary(Of String, Object) From {{command.ToString, cancelledOrder}}
                Case ExecutionCommands.GetOrderHistory
                    Dim orderList As List(Of Order) = Nothing
                    If stockData IsNot Nothing AndAlso stockData.Count > 0 Then
                        orderList = Await Task.Factory.StartNew(Function()
                                                                    Try
                                                                        Return _Kite.GetOrderHistory(OrderId:=CType(stockData("OrderId"), String))
                                                                    Catch ex As Exception
                                                                        logger.Error(ex)
                                                                        lastException = ex
                                                                        Return Nothing
                                                                    End Try
                                                                End Function).ConfigureAwait(False)
                        _cts.Token.ThrowIfCancellationRequested()
                    End If
                    ret = New Dictionary(Of String, Object) From {{command.ToString, orderList}}
                Case ExecutionCommands.GetOrders
                    Dim orderList As List(Of Order) = Nothing
                    orderList = Await Task.Factory.StartNew(Function()
                                                                Try
                                                                    Return _Kite.GetOrders()
                                                                Catch ex As Exception
                                                                    logger.Error(ex)
                                                                    lastException = ex
                                                                    Return Nothing
                                                                End Try
                                                            End Function).ConfigureAwait(False)
                    _cts.Token.ThrowIfCancellationRequested()
                    ret = New Dictionary(Of String, Object) From {{command.ToString, orderList}}
                Case ExecutionCommands.GetOrderTrades
                    Dim tradeList As List(Of Trade) = Nothing
                    If stockData IsNot Nothing AndAlso stockData.Count > 0 Then
                        tradeList = Await Task.Factory.StartNew(Function()
                                                                    Try
                                                                        Return _Kite.GetOrderTrades(OrderId:=CType(stockData("OrderId"), String))
                                                                    Catch ex As Exception
                                                                        logger.Error(ex)
                                                                        lastException = ex
                                                                        Return Nothing
                                                                    End Try
                                                                End Function).ConfigureAwait(False)
                        _cts.Token.ThrowIfCancellationRequested()
                    Else
                        tradeList = Await Task.Factory.StartNew(Function()
                                                                    Try
                                                                        Return _Kite.GetOrderTrades()
                                                                    Catch ex As Exception
                                                                        logger.Error(ex)
                                                                        lastException = ex
                                                                        Return Nothing
                                                                    End Try
                                                                End Function).ConfigureAwait(False)
                        _cts.Token.ThrowIfCancellationRequested()
                    End If
                    ret = New Dictionary(Of String, Object) From {{command.ToString, tradeList}}
                Case ExecutionCommands.GetInstruments
                    Dim instruments As List(Of Instrument) = Nothing
                    instruments = Await Task.Factory.StartNew(Function()
                                                                  Try
                                                                      Return _Kite.GetInstruments()
                                                                  Catch ex As Exception
                                                                      logger.Error(ex)
                                                                      lastException = ex
                                                                      Return Nothing
                                                                  End Try
                                                              End Function).ConfigureAwait(False)
                    _cts.Token.ThrowIfCancellationRequested()
                    Dim count As Integer = If(instruments Is Nothing, 0, instruments.Count)
                    logger.Debug(String.Format("Fetched {0} instruments from Zerodha", count))
                    If instruments IsNot Nothing AndAlso instruments.Count > 0 Then
                        instruments.RemoveAll(Function(x)
                                                  Return x.Exchange = "BFO" Or x.Exchange = "BSE"
                                              End Function)
                        'instruments.RemoveAll(Function(x)
                        '                          Return x.Segment.EndsWith("OPT")
                        '                      End Function)
                        instruments.RemoveAll(Function(x)
                                                  Return x.TradingSymbol.Length > 3 AndAlso x.TradingSymbol.Substring(x.TradingSymbol.Length - 3).StartsWith("-")
                                              End Function)
                        count = If(instruments Is Nothing, 0, instruments.Count)
                        logger.Debug(String.Format("After cleanup, fetched {0} instruments from Zerodha", count))
                    End If
                    _cts.Token.ThrowIfCancellationRequested()
                    ret = New Dictionary(Of String, Object) From {{command.ToString, instruments}}
                Case ExecutionCommands.GetUserMargins
                    Dim userMargins As UserMarginsResponse = Nothing
                    userMargins = Await Task.Factory.StartNew(Function()
                                                                  Try
                                                                      Return _Kite.GetMargins()
                                                                  Catch ex As Exception
                                                                      logger.Error(ex)
                                                                      lastException = ex
                                                                      Return Nothing
                                                                  End Try
                                                              End Function).ConfigureAwait(False)
                    _cts.Token.ThrowIfCancellationRequested()
                    ret = New Dictionary(Of String, Object) From {{command.ToString, userMargins}}
                Case ExecutionCommands.InvalidateAccessToken
                    Dim invalidateToken = _Kite.InvalidateAccessToken(CType(ParentController.APIConnection, ZerodhaConnection).AccessToken)
                    lastException = Nothing
                    _cts.Token.ThrowIfCancellationRequested()
                Case Else
                    Throw New ApplicationException("No Command Triggered")
            End Select
            If lastException IsNot Nothing Then
                Throw lastException
            End If
            Return ret
        End Function
#End Region

    End Class
End Namespace
