Imports System.Net.Http
Imports System.Threading
Imports Algo2TradeCore.Adapter
Imports Algo2TradeCore.Adapter.APIAdapter
Imports Algo2TradeCore.Entities
Imports Algo2TradeCore.Strategies
Imports Algo2TradeCore.Exceptions
Imports NLog
Imports Utilities
Imports Utilities.ErrorHandlers
Imports Algo2TradeCore.ChartHandler.ChartStyle
Imports Algo2TradeCore.Entities.UserSettings
Imports System.IO
Imports System.Runtime.Serialization.Formatters.Binary

Namespace Controller
    Public MustInherit Class APIStrategyController

#Region "Events/Event handlers"
        Public Event DocumentDownloadComplete()
        Public Event DocumentRetryStatus(ByVal currentTry As Integer, ByVal totalTries As Integer)
        Public Event Heartbeat(ByVal msg As String)
        Public Event WaitingFor(ByVal elapsedSecs As Integer, ByVal totalSecs As Integer, ByVal msg As String)
        Public Event DocumentDownloadCompleteEx(ByVal source As List(Of Object))
        Public Event DocumentRetryStatusEx(ByVal currentTry As Integer, ByVal totalTries As Integer, ByVal source As List(Of Object))
        Public Event HeartbeatEx(ByVal msg As String, ByVal source As List(Of Object))
        Public Event WaitingForEx(ByVal elapsedSecs As Integer, ByVal totalSecs As Integer, ByVal msg As String, ByVal source As List(Of Object))
        'Create the events for UI to handle the way it needs to show the ticker
        Public Event TickerConnect()
        Public Event TickerClose()
        Public Event TickerErrorWithStatus(ByVal isConnected As Boolean, ByVal errorMessage As String)
        Public Event TickerError(ByVal errorMessage As String)
        Public Event TickerNoReconnect()
        Public Event TickerReconnect()
        Public Event FetcherError(ByVal instrumentIdentifier As String, ByVal errorMessage As String)
        Public Event CollectorError(ByVal errorMessage As String)
        Public Event NewItemAdded(ByVal item As ActivityDashboard)
        Public Event SessionExpiry(ByVal runningStrategy As Strategy)

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
        'The below functions are needed to allow the derived classes to raise the above two events
        Protected Overridable Sub OnDocumentDownloadCompleteEx(ByVal source As List(Of Object))
            RaiseEvent DocumentDownloadCompleteEx(source)
        End Sub
        Protected Overridable Sub OnDocumentRetryStatusEx(ByVal currentTry As Integer, ByVal totalTries As Integer, ByVal source As List(Of Object))
            RaiseEvent DocumentRetryStatusEx(currentTry, totalTries, source)
        End Sub
        Protected Overridable Sub OnHeartbeatEx(ByVal msg As String, ByVal source As List(Of Object))
            RaiseEvent HeartbeatEx(msg, source)
        End Sub
        Protected Overridable Sub OnWaitingForEx(ByVal elapsedSecs As Integer, ByVal totalSecs As Integer, ByVal msg As String, ByVal source As List(Of Object))
            RaiseEvent WaitingForEx(elapsedSecs, totalSecs, msg, source)
        End Sub
        Public Overridable Sub OnTickerConnect()
            RaiseEvent TickerConnect()
        End Sub
        Public Overridable Sub OnTickerClose()
            RaiseEvent TickerClose()
        End Sub
        Public Overridable Sub OnTickerError(ByVal errorMessage As String)
            RaiseEvent TickerError(errorMessage)
        End Sub
        Public Overridable Sub OnTickerErrorWithStatus(ByVal isConnected As Boolean, ByVal errorMessage As String)
            RaiseEvent TickerErrorWithStatus(isConnected, errorMessage)
        End Sub
        Public Overridable Sub OnTickerNoReconnect()
            RaiseEvent TickerNoReconnect()
        End Sub
        Public Overridable Sub OnTickerReconnect()
            RaiseEvent TickerReconnect()
        End Sub
        Public Overridable Sub OnFetcherError(ByVal instrumentIdentifier As String, ByVal errorMessage As String)
            RaiseEvent FetcherError(instrumentIdentifier, errorMessage)
        End Sub
        Public Overridable Sub OnCollectorError(ByVal errorMessage As String)
            RaiseEvent CollectorError(errorMessage)
        End Sub
        Protected Overridable Sub OnNewItemAdded(ByVal item As ActivityDashboard)
            If item IsNot Nothing Then
                RaiseEvent NewItemAdded(item)
            End If
        End Sub
        Protected Sub OnSessionExpiry(ByVal runningStrategy As Strategy)
            RaiseEvent SessionExpiry(runningStrategy)
        End Sub
#End Region

#Region "Logging and Status Progress"
        Public Shared logger As Logger = LogManager.GetCurrentClassLogger
#End Region

        Protected _currentUser As IUser
        Protected _cts As CancellationTokenSource
        Protected _MaxReTries As Integer = 20
        Protected _WaitDurationOnConnectionFailure As TimeSpan = TimeSpan.FromSeconds(5)
        Protected _WaitDurationOnServiceUnavailbleFailure As TimeSpan = TimeSpan.FromSeconds(30)
        Protected _WaitDurationOnAnyFailure As TimeSpan = TimeSpan.FromSeconds(10)
        Protected _LoginURL As String
        Protected _LoginThreads As Integer
        Public Property APIConnection As IConnection
        Public ReadOnly Property BrokerSource As APISource
        Public Property OrphanException As Exception
        Public Property InstrumentMappingTable As Concurrent.ConcurrentDictionary(Of String, String)
        Public Property UserInputs As ControllerUserInputs

        Protected _APIAdapter As APIAdapter
        Protected _APITicker As APITicker
        Protected _APIHistoricalDataFetcher As APIHistoricalDataFetcher
        Protected _APIInformationCollector As APIInformationCollector
        Protected _AllInstruments As IEnumerable(Of IInstrument)
        Protected _AllStrategyUniqueInstruments As IEnumerable(Of IInstrument)
        Protected _AllStrategies As List(Of Strategy)
        Protected _subscribedStrategyInstruments As Dictionary(Of String, Concurrent.ConcurrentBag(Of StrategyInstrument))
        Protected _rawPayloadCreators As Dictionary(Of String, CandleStickChart)
        Protected _userMarginFilename As String
        Private _lastGetOrderTime As Date
        Public Sub New(ByVal validatedUser As IUser,
                       ByVal associatedBrokerSource As APISource,
                       ByVal associatedUserInputs As ControllerUserInputs,
                       ByVal canceller As CancellationTokenSource)
            _currentUser = validatedUser
            Me.BrokerSource = associatedBrokerSource
            UserInputs = associatedUserInputs
            _cts = canceller
            _LoginThreads = 0
            _userMarginFilename = Path.Combine(My.Application.Info.DirectoryPath, String.Format("UserMargin_{0}.Margin.a2t", Now.ToString("yy_MM_dd")))
        End Sub
        Public MustOverride Function GetErrorResponse(ByVal response As Object) As String
        Public MustOverride Function CreateDummySingleInstrument(ByVal supportedTradingSymbol As String, ByVal instrumentToken As UInteger, ByVal sampleInstrument As IInstrument) As IInstrument
        Public MustOverride Async Function CloseTickerIfConnectedAsync() As Task
        Public MustOverride Async Function CloseFetcherIfConnectedAsync(ByVal forceClose As Boolean) As Task
        Public MustOverride Async Function CloseCollectorIfConnectedAsync(ByVal forceClose As Boolean) As Task

        Public Sub RefreshCancellationToken(ByVal canceller As CancellationTokenSource)
            _cts = canceller
            If _APITicker IsNot Nothing Then _APITicker.RefreshCancellationToken(canceller)
            If _APIHistoricalDataFetcher IsNot Nothing Then _APIHistoricalDataFetcher.RefreshCancellationToken(canceller)
            If _APIInformationCollector IsNot Nothing Then _APIInformationCollector.RefreshCancellationToken(canceller)
        End Sub

#Region "Execute Command"
        Protected Async Function ExecuteCommandAsync(ByVal command As APIAdapter.ExecutionCommands, ByVal data As Object) As Task(Of Object)
            'logger.Debug("ExecuteCommandAsync, parameters:{0},{1}", command, Utilities.Strings.JsonSerialize(data))

            Dim ret As Object = Nothing
            Dim lastException As Exception = Nothing
            Dim allOKWithoutException As Boolean = False

            Using Waiter As New Waiter(_cts)
                AddHandler Waiter.Heartbeat, AddressOf OnHeartbeat
                AddHandler Waiter.WaitingFor, AddressOf OnWaitingFor
                Dim apiConnectionBeingUsed As IConnection = Me.APIConnection
                For retryCtr = 1 To _MaxReTries
                    _cts.Token.ThrowIfCancellationRequested()
                    lastException = Nothing
                    While Me.APIConnection Is Nothing OrElse apiConnectionBeingUsed Is Nothing OrElse
                       (Me.APIConnection IsNot Nothing AndAlso apiConnectionBeingUsed IsNot Nothing AndAlso
                       Not Me.APIConnection.Equals(apiConnectionBeingUsed))
                        apiConnectionBeingUsed = Me.APIConnection
                        _cts.Token.ThrowIfCancellationRequested()
                        If command <> ExecutionCommands.GetOrders AndAlso command <> ExecutionCommands.GetHoldings Then
                            logger.Debug("Waiting for fresh token before running command:{0}", command.ToString)
                        End If
                        Await Task.Delay(500, _cts.Token).ConfigureAwait(False)
                        _cts.Token.ThrowIfCancellationRequested()
                    End While

                    _APIAdapter.SetAPIAccessToken(APIConnection.AccessToken)

                    If command <> ExecutionCommands.GetOrders AndAlso command <> ExecutionCommands.GetHoldings Then
                        logger.Debug("Firing command:{0}", command.ToString)
                    End If
                    OnDocumentRetryStatus(retryCtr, _MaxReTries)
                    Try
                        _cts.Token.ThrowIfCancellationRequested()
                        Select Case command
                            Case ExecutionCommands.GetInstruments
                                Dim allInstrumentsResponse As IEnumerable(Of IInstrument) = Nothing
                                allInstrumentsResponse = Await _APIAdapter.GetAllInstrumentsAsync().ConfigureAwait(False)
                                If allInstrumentsResponse IsNot Nothing Then
                                    logger.Debug("Getting instruments is complete, allInstrumentsResponse.count:{0}", allInstrumentsResponse.Count)
                                    lastException = Nothing
                                    allOKWithoutException = True
                                    _cts.Token.ThrowIfCancellationRequested()
                                    ret = allInstrumentsResponse
                                    _cts.Token.ThrowIfCancellationRequested()
                                    Exit For
                                Else
                                    Throw New ApplicationException(String.Format("Getting all instruments for the day did not succeed"))
                                End If
                            Case ExecutionCommands.GetQuotes
                                Dim allQuotesResponse As IEnumerable(Of IQuote) = Nothing
                                allQuotesResponse = Await _APIAdapter.GetAllQuotesAsync(data).ConfigureAwait(False)
                                If allQuotesResponse IsNot Nothing Then
                                    logger.Debug("Getting all quotes is complete, allQuotesResponse.count:{0}", allQuotesResponse.Count)
                                    lastException = Nothing
                                    allOKWithoutException = True
                                    _cts.Token.ThrowIfCancellationRequested()
                                    ret = allQuotesResponse
                                    _cts.Token.ThrowIfCancellationRequested()
                                    Exit For
                                Else
                                    Throw New ApplicationException(String.Format("Getting all quotes did not succeed"))
                                End If
                            Case ExecutionCommands.GetOrders
                                If DateDiff(DateInterval.Second, _lastGetOrderTime, Now) < 1 Then
                                    Await Task.Delay(1000, _cts.Token).ConfigureAwait(False)
                                End If
                                _lastGetOrderTime = Now
                                Dim allOrderResponse As IEnumerable(Of IOrder) = Nothing
                                allOrderResponse = Await _APIAdapter.GetAllOrdersAsync().ConfigureAwait(False)
                                If allOrderResponse IsNot Nothing Then
                                    'logger.Debug("Getting all orders is complete, allOrdersResponse.count:{0}", allOrderResponse.Count)
                                Else
                                    'logger.Debug("Getting all orders is complete, allOrdersResponse.count:{0}", 0)
                                End If
                                lastException = Nothing
                                allOKWithoutException = True
                                _cts.Token.ThrowIfCancellationRequested()
                                ret = allOrderResponse
                                _cts.Token.ThrowIfCancellationRequested()
                                Exit For
                            Case ExecutionCommands.GetHoldings
                                Dim allHoldingResponse As IEnumerable(Of IHolding) = Nothing
                                allHoldingResponse = Await _APIAdapter.GetAllHoldingsAsync().ConfigureAwait(False)
                                If allHoldingResponse IsNot Nothing Then
                                    'logger.Debug("Getting all holdings is complete, allHoldingResponse.count:{0}", allHoldingResponse.Count)
                                Else
                                    'logger.Debug("Getting all holdings is complete, allHoldingResponse.count:{0}", 0)
                                End If
                                lastException = Nothing
                                allOKWithoutException = True
                                _cts.Token.ThrowIfCancellationRequested()
                                ret = allHoldingResponse
                                _cts.Token.ThrowIfCancellationRequested()
                                Exit For
                            Case ExecutionCommands.GetUserMargins
                                Dim userMarginResponse As Dictionary(Of Enums.TypeOfExchage, IUserMargin) = Nothing
                                userMarginResponse = Await _APIAdapter.GetUserMarginsAsync.ConfigureAwait(False)
                                If userMarginResponse IsNot Nothing AndAlso userMarginResponse.Count > 0 Then
                                    logger.Debug("Getting userMarginResponse is complete, count:{0}", userMarginResponse.Count)
                                    lastException = Nothing
                                    allOKWithoutException = True
                                    _cts.Token.ThrowIfCancellationRequested()
                                    ret = userMarginResponse
                                    _cts.Token.ThrowIfCancellationRequested()
                                    Exit For
                                Else
                                    Throw New ApplicationException(String.Format("Getting user margin did not succeed"))
                                End If
                        End Select
                    Catch aex As AdapterBusinessException
                        logger.Error(aex)
                        lastException = aex
                        Select Case aex.ExceptionType
                            Case AdapterBusinessException.TypeOfException.TokenException
                                Continue For
                            Case AdapterBusinessException.TypeOfException.DataException
                                Continue For
                            Case AdapterBusinessException.TypeOfException.NetworkException
                                Continue For
                            Case Else
                                Exit For
                        End Select
                    Catch opx As OperationCanceledException
                        logger.Error(opx)
                        lastException = opx
                        If Not _cts.Token.IsCancellationRequested Then
                            _cts.Token.ThrowIfCancellationRequested()
                            If Not Waiter.WaitOnInternetFailure(_WaitDurationOnConnectionFailure) Then
                                'Provide required wait in case internet was already up
                                logger.Debug("HTTP->Task was cancelled without internet problem:{0}",
                                             opx.Message)
                                _cts.Token.ThrowIfCancellationRequested()
                                Waiter.SleepRequiredDuration(_WaitDurationOnAnyFailure.TotalSeconds, "Non-explicit cancellation")
                                _cts.Token.ThrowIfCancellationRequested()
                            Else
                                logger.Debug("HTTP->Task was cancelled due to internet problem:{0}, waited prescribed seconds, will now retry",
                                             opx.Message)
                                'Since internet was down, no need to consume retries
                                retryCtr -= 1
                            End If
                        End If
                    Catch hex As HttpRequestException
                        logger.Error(hex)
                        lastException = hex
                        If ExceptionExtensions.GetExceptionMessages(hex).Contains("trust relationship") Then
                            Throw New ForbiddenException(hex.Message, hex, ForbiddenException.TypeOfException.PossibleReloginRequired)
                        End If
                        _cts.Token.ThrowIfCancellationRequested()
                        If Not Waiter.WaitOnInternetFailure(_WaitDurationOnConnectionFailure) Then
                            If hex.Message.Contains("429") Or hex.Message.Contains("503") Then
                                logger.Debug("HTTP->429/503 error without internet problem:{0}",
                                             hex.Message)
                                _cts.Token.ThrowIfCancellationRequested()
                                Waiter.SleepRequiredDuration(_WaitDurationOnServiceUnavailbleFailure.TotalSeconds, "Service unavailable(429/503)")
                                _cts.Token.ThrowIfCancellationRequested()
                                'Since site service is blocked, no need to consume retries
                                retryCtr -= 1
                            ElseIf hex.Message.Contains("404") Then
                                logger.Debug("HTTP->404 error without internet problem:{0}",
                                             hex.Message)
                                _cts.Token.ThrowIfCancellationRequested()
                                'No point retrying, exit for
                                Exit For
                            Else
                                If ExceptionExtensions.IsExceptionConnectionRelated(hex) Then
                                    logger.Debug("HTTP->HttpRequestException without internet problem but of type internet related detected:{0}",
                                                 hex.Message)
                                    _cts.Token.ThrowIfCancellationRequested()
                                    Waiter.SleepRequiredDuration(_WaitDurationOnConnectionFailure.TotalSeconds, "Connection HttpRequestException")
                                    _cts.Token.ThrowIfCancellationRequested()
                                    'Since exception was internet related, no need to consume retries
                                    retryCtr -= 1
                                Else
                                    'Provide required wait in case internet was already up
                                    logger.Debug("HTTP->HttpRequestException without internet problem:{0}",
                                                 hex.Message)
                                    _cts.Token.ThrowIfCancellationRequested()
                                    Waiter.SleepRequiredDuration(_WaitDurationOnAnyFailure.TotalSeconds, "Unknown HttpRequestException:" & hex.Message)
                                    _cts.Token.ThrowIfCancellationRequested()
                                End If
                            End If
                        Else
                            logger.Debug("HTTP->HttpRequestException with internet problem:{0}, waited prescribed seconds, will now retry",
                                         hex.Message)
                            'Since internet was down, no need to consume retries
                            retryCtr -= 1
                        End If
                    Catch ex As Exception
                        logger.Error(ex)
                        lastException = ex
                        'Exit if it is a network failure check and stop retry to avoid stack overflow
                        'Need to relogin, no point retrying
                        If ExceptionExtensions.GetExceptionMessages(ex).Contains("disposed") Then
                            Throw New ForbiddenException(ex.Message, ex, ForbiddenException.TypeOfException.ExceptionInBetweenLoginProcess)
                        End If
                        _cts.Token.ThrowIfCancellationRequested()
                        If Not Waiter.WaitOnInternetFailure(_WaitDurationOnConnectionFailure) Then
                            'Provide required wait in case internet was already up
                            _cts.Token.ThrowIfCancellationRequested()
                            If ExceptionExtensions.IsExceptionConnectionRelated(ex) Then
                                logger.Debug("HTTP->Exception without internet problem but of type internet related detected:{0}",
                                             ex.Message)
                                _cts.Token.ThrowIfCancellationRequested()
                                Waiter.SleepRequiredDuration(_WaitDurationOnConnectionFailure.TotalSeconds, "Connection Exception")
                                _cts.Token.ThrowIfCancellationRequested()
                                'Since exception was internet related, no need to consume retries
                                retryCtr -= 1
                            Else
                                logger.Debug("HTTP->Exception without internet problem of unknown type detected:{0}",
                                             ex.Message)
                                _cts.Token.ThrowIfCancellationRequested()
                                Waiter.SleepRequiredDuration(_WaitDurationOnAnyFailure.TotalSeconds, "Unknown Exception")
                                _cts.Token.ThrowIfCancellationRequested()
                            End If
                        Else
                            logger.Debug("HTTP->Exception with internet problem:{0}, waited prescribed seconds, will now retry",
                                         ex.Message)
                            'Since internet was down, no need to consume retries
                            retryCtr -= 1
                        End If
                    Finally
                        OnDocumentDownloadComplete()
                    End Try
                    _cts.Token.ThrowIfCancellationRequested()
                    If ret IsNot Nothing Then
                        Exit For
                    End If
                    GC.Collect()
                Next
                RemoveHandler Waiter.Heartbeat, AddressOf OnHeartbeat
                RemoveHandler Waiter.WaitingFor, AddressOf OnWaitingFor
            End Using
            _cts.Token.ThrowIfCancellationRequested()
            If Not allOKWithoutException Then Throw lastException
            Return ret
        End Function
#End Region

        Protected MustOverride Function GetLoginURL() As String
        Public MustOverride Async Function LoginAsync() As Task(Of IConnection)
        Public MustOverride Async Function PrepareToRunStrategyAsync() As Task(Of Boolean)
        Protected MustOverride Async Function FillQuantityMultiplierMapAsync() As Task
        Public MustOverride Async Function SubscribeStrategyAsync(ByVal strategyToRun As Strategy) As Task
        Public MustOverride Overloads Async Function GetOrderDetailsAsync() As Task(Of Concurrent.ConcurrentBag(Of IBusinessOrder))
        Public MustOverride Overloads Async Function GetHoldingDetailsAsync() As Task(Of Concurrent.ConcurrentBag(Of IHolding))
        Public Sub FillCandlestickCreator()
            If _AllStrategyUniqueInstruments IsNot Nothing AndAlso _AllStrategyUniqueInstruments.Count > 0 Then
                For Each runningStrategyUniqueInstruments In _AllStrategyUniqueInstruments
                    _cts.Token.ThrowIfCancellationRequested()
                    If _rawPayloadCreators IsNot Nothing AndAlso _rawPayloadCreators.ContainsKey(runningStrategyUniqueInstruments.InstrumentIdentifier) Then
                        Continue For
                    End If
                    Dim candleStickBasedStrategyInstruments As IEnumerable(Of StrategyInstrument) =
                        _subscribedStrategyInstruments(runningStrategyUniqueInstruments.InstrumentIdentifier).Where(Function(x)
                                                                                                                        Return x.ParentStrategy.IsStrategyCandleStickBased
                                                                                                                    End Function)

                    If candleStickBasedStrategyInstruments IsNot Nothing AndAlso candleStickBasedStrategyInstruments.Count > 0 Then
                        If _rawPayloadCreators Is Nothing Then _rawPayloadCreators = New Dictionary(Of String, CandleStickChart)
                        _rawPayloadCreators.Add(runningStrategyUniqueInstruments.InstrumentIdentifier,
                                                New CandleStickChart(Me,
                                                                     runningStrategyUniqueInstruments,
                                                                     candleStickBasedStrategyInstruments,
                                                                     _cts))
                        If runningStrategyUniqueInstruments.RawPayloads Is Nothing Then runningStrategyUniqueInstruments.RawPayloads = New Concurrent.ConcurrentDictionary(Of Date, OHLCPayload)
                        If runningStrategyUniqueInstruments.TickPayloads Is Nothing Then runningStrategyUniqueInstruments.TickPayloads = New Concurrent.ConcurrentBag(Of ITick)
                    End If
                Next
            End If
        End Sub
        Protected Overridable Async Function FillOrderDetailsAsync() As Task
            Try
                _cts.Token.ThrowIfCancellationRequested()
                Dim orderDetails As Concurrent.ConcurrentBag(Of IBusinessOrder) = Await GetOrderDetailsAsync().ConfigureAwait(False)
                If orderDetails IsNot Nothing AndAlso orderDetails.Count > 0 Then
                    For Each orderData In orderDetails
                        _cts.Token.ThrowIfCancellationRequested()
                        If _AllStrategies IsNot Nothing AndAlso _AllStrategies.Count > 0 Then
                            For Each strategyToRun In _AllStrategies
                                _cts.Token.ThrowIfCancellationRequested()
                                Await strategyToRun.ProcessOrderAsync(orderData).ConfigureAwait(False)
                            Next
                        End If
                    Next
                End If
            Catch cex As OperationCanceledException
                logger.Warn(cex)
                Me.OrphanException = cex
            Catch ex As Exception
                'Neglect error as in the next minute, it will be run again,
                'till that time tick based candles will be used
                logger.Warn(ex)
            End Try
        End Function
        Protected Async Function CreateInstrumentMappingTable() As Task
            Await Task.Delay(0).ConfigureAwait(False)
            Dim filePath As String = Path.Combine(My.Application.Info.DirectoryPath, String.Format("InstrumentMappingFile_{0}.Instruments.a2t", Now.ToString("yy_MM_dd")))
            Dim mapStartNumber As Integer = 0

            If InstrumentMappingTable Is Nothing Then InstrumentMappingTable = New Concurrent.ConcurrentDictionary(Of String, String)
            If _AllStrategyUniqueInstruments IsNot Nothing AndAlso _AllStrategyUniqueInstruments.Count > 0 Then
                Dim instrumentMappingTableFromDisk As Concurrent.ConcurrentDictionary(Of String, String) = Nothing
                If File.Exists(filePath) Then
                    instrumentMappingTableFromDisk = Utilities.Strings.DeserializeToCollection(Of Concurrent.ConcurrentDictionary(Of String, String))(filePath)
                    Dim needToBeMapped As List(Of IInstrument) = Nothing
                    For Each runningInstrument In _AllStrategyUniqueInstruments
                        If instrumentMappingTableFromDisk IsNot Nothing AndAlso instrumentMappingTableFromDisk.Count > 0 AndAlso
                                instrumentMappingTableFromDisk.ContainsKey(runningInstrument.InstrumentIdentifier) Then
                            InstrumentMappingTable.GetOrAdd(runningInstrument.InstrumentIdentifier, instrumentMappingTableFromDisk(runningInstrument.InstrumentIdentifier))
                        Else
                            If needToBeMapped Is Nothing Then needToBeMapped = New List(Of IInstrument)
                            needToBeMapped.Add(runningInstrument)
                        End If
                    Next
                    If needToBeMapped IsNot Nothing AndAlso needToBeMapped.Count > 0 Then
                        For Each runningInstrument In needToBeMapped
                            Dim mapNumber As Integer = Integer.MinValue
                            For numberList = 0 To 999
                                Dim numberToBeChecked As Integer = numberList
                                Dim mappedData As IEnumerable(Of KeyValuePair(Of String, String)) = InstrumentMappingTable.Where(Function(x)
                                                                                                                                     Return x.Value = numberToBeChecked
                                                                                                                                 End Function)
                                If mappedData Is Nothing OrElse mappedData.Count = 0 Then
                                    mapNumber = numberToBeChecked
                                    Exit For
                                End If
                            Next
                            InstrumentMappingTable.GetOrAdd(runningInstrument.InstrumentIdentifier, mapNumber)
                        Next
                    End If
                Else
                    For Each runningInstrument In _AllStrategyUniqueInstruments
                        If mapStartNumber > 999 Then
                            Throw New ApplicationException("Can not subscribe more than 999 instruments")
                        End If
                        InstrumentMappingTable.GetOrAdd(runningInstrument.InstrumentIdentifier, mapStartNumber)
                        mapStartNumber += 1
                    Next
                End If
            End If
            For Each runningFile In Directory.GetFiles(My.Application.Info.DirectoryPath, "InstrumentMappingFile*.Instruments.a2t")
                Try
                    File.Delete(runningFile)
                Catch ex As Exception
                    If runningFile.ToUpper.Equals(filePath.ToUpper) Then
                        Throw New ApplicationException(String.Format("Cannot create instrument mapping file due to:{0}", ex.Message), ex)
                    End If
                End Try
            Next
            'Serialize collection
            Utilities.Strings.SerializeFromCollection(Of Concurrent.ConcurrentDictionary(Of String, String))(filePath, InstrumentMappingTable)
        End Function
        Protected Overridable Async Function FillUserStartingMargin(ByVal filename As String) As Task
            _cts.Token.ThrowIfCancellationRequested()
            If File.Exists(filename) Then
                _currentUser.DaysStartingCapitals = Utilities.Strings.DeserializeToCollection(Of IUser)(filename).DaysStartingCapitals
            Else
                Dim execCommand As ExecutionCommands = ExecutionCommands.GetUserMargins
                Dim userMarginResponses As Dictionary(Of Enums.TypeOfExchage, IUserMargin) = Await ExecuteCommandAsync(execCommand, Nothing).ConfigureAwait(False)
                _cts.Token.ThrowIfCancellationRequested()
                If userMarginResponses IsNot Nothing AndAlso userMarginResponses.Count > 0 Then
                    _cts.Token.ThrowIfCancellationRequested()
                    For Each userMarginResponse In userMarginResponses
                        _cts.Token.ThrowIfCancellationRequested()
                        If _currentUser.DaysStartingCapitals Is Nothing Then _currentUser.DaysStartingCapitals = New Dictionary(Of Enums.TypeOfExchage, Decimal)
                        _currentUser.DaysStartingCapitals.Add(userMarginResponse.Key, userMarginResponse.Value.NetAmount)
                    Next
                End If
                Utilities.Strings.SerializeFromCollection(Of IUser)(_userMarginFilename, _currentUser)
            End If
        End Function
        Public Function GetUserMargin() As Dictionary(Of Enums.TypeOfExchage, Decimal)
            Return _currentUser.DaysStartingCapitals
        End Function
        Public Function GetChartCreator(ByVal instrumentIdentifier As String) As Chart
            If instrumentIdentifier IsNot Nothing AndAlso
                _rawPayloadCreators IsNot Nothing AndAlso
                _rawPayloadCreators.Count > 0 AndAlso
                _rawPayloadCreators.ContainsKey(instrumentIdentifier) Then
                Return _rawPayloadCreators(instrumentIdentifier)
            Else
                Return Nothing
            End If
        End Function
    End Class
End Namespace