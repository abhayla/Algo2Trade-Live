Imports System.Net
Imports System.Threading
Imports Algo2TradeCore.Controller
Imports NLog
Imports Algo2TradeCore.Entities
Imports System.IO
Imports Utilities.Strings

Namespace Adapter
    Public Class ZerodhaHistoricalDataFetcher
        Inherits APIHistoricalDataFetcher
        Implements IDisposable

#Region "Logging and Status Progress"
        Public Shared Shadows logger As Logger = LogManager.GetCurrentClassLogger
#End Region

#Region "Events/Event handlers specific to the derived class"
        Public Event FetcherCandles(ByVal instrumentIdentifier As String, ByVal historicalCandlesJSONDict As Dictionary(Of String, Object))
        Public Event FetcherError(ByVal instrumentIdentifier As String, ByVal msg As String)
        'The below functions are needed to allow the derived classes to raise the above two events
        Protected Overridable Async Function OnFetcherCandlesAsync(ByVal instrumentIdentifier As String, ByVal historicalCandlesJSONDict As Dictionary(Of String, Object)) As Task
            Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
            RaiseEvent FetcherCandles(instrumentIdentifier, historicalCandlesJSONDict)
        End Function
        Protected Overridable Sub OnFetcherError(ByVal instrumentIdentifier As String, ByVal msg As String)
            RaiseEvent FetcherError(instrumentIdentifier, msg)
        End Sub
#End Region

        'Private ZERODHA_HISTORICAL_URL = "https://kitecharts-aws.zerodha.com/api/chart/{0}/minute?api_key=kitefront&access_token=K&from={1}&to={2}"
        Private ZERODHA_HISTORICAL_URL = "https://kite.zerodha.com/oms/instruments/historical/{0}/minute?oi=1&from={1}&to={2}"
        Public Sub New(ByVal associatedParentController As APIStrategyController,
                       ByVal daysToGoBack As Integer,
                       ByVal canceller As CancellationTokenSource)
            MyBase.New(associatedParentController, daysToGoBack, canceller)
            StartPollingAsync()
        End Sub
        Public Sub New(ByVal associatedParentController As APIStrategyController,
                       ByVal daysToGoBack As Integer,
                       ByVal instrumentIdentifier As String,
                       ByVal canceller As CancellationTokenSource)
            MyBase.New(associatedParentController, daysToGoBack, instrumentIdentifier, canceller)
            Dim currentZerodhaStrategyController As ZerodhaStrategyController = CType(ParentController, ZerodhaStrategyController)
            AddHandler Me.FetcherCandles, AddressOf currentZerodhaStrategyController.OnFetcherCandlesAsync
            AddHandler Me.FetcherError, AddressOf currentZerodhaStrategyController.OnFetcherError
        End Sub
        Public Overrides Async Function ConnectFetcherAsync() As Task
            'logger.Debug("{0}->ConnectTickerAsync, parameters:Nothing", Me.ToString)
            _cts.Token.ThrowIfCancellationRequested()
            Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
            'Dim currentZerodhaStrategyController As ZerodhaStrategyController = CType(ParentController, ZerodhaStrategyController)

            'RemoveHandler Me.FetcherCandlesAsync, AddressOf currentZerodhaStrategyController.OnFetcherCandlesAsync
            'RemoveHandler Me.FetcherError, AddressOf currentZerodhaStrategyController.OnFetcherError
            '_cts.Token.ThrowIfCancellationRequested()
            'AddHandler Me.FetcherCandlesAsync, AddressOf currentZerodhaStrategyController.OnFetcherCandlesAsync
            'AddHandler Me.FetcherError, AddressOf currentZerodhaStrategyController.OnFetcherError
        End Function
        'Protected Overrides Async Function GetHistoricalCandleStickAsync() As Task(Of Dictionary(Of String, Object))
        '    Try
        '        'If Not _isPollRunning Then Exit Function
        '        _cts.Token.ThrowIfCancellationRequested()
        '        Dim historicalDataURL As String = String.Format(ZERODHA_HISTORICAL_URL,
        '                                                            _instrumentIdentifer,
        '                                                            Now.AddDays(-1 * _daysToGoBack).ToString("yyyy-MM-dd"),
        '                                                            Now.ToString("yyyy-MM-dd"))

        '        Console.WriteLine(historicalDataURL)
        '        Using sr = New StreamReader(HttpWebRequest.Create(historicalDataURL).GetResponseAsync().Result.GetResponseStream)

        '            Dim jsonString = Await sr.ReadToEndAsync.ConfigureAwait(False)
        '            Dim retDictionary As Dictionary(Of String, Object) = StringManipulation.JsonDeserialize(jsonString)

        '            Return retDictionary
        '        End Using
        '    Catch ex As Exception
        '        Throw ex
        '    End Try
        'End Function

        Protected Overrides Async Function GetHistoricalCandleStickAsync() As Task(Of Dictionary(Of String, Object))
            Try
                _cts.Token.ThrowIfCancellationRequested()
                Dim historicalDataURL As String = String.Format(ZERODHA_HISTORICAL_URL,
                                                                    _instrumentIdentifer,
                                                                    Now.AddDays(-1 * _daysToGoBack).ToString("yyyy-MM-dd"),
                                                                    Now.ToString("yyyy-MM-dd"))

                Console.WriteLine(historicalDataURL)
                Dim request As HttpWebRequest = HttpWebRequest.Create(historicalDataURL)
                request.Host = "kite.zerodha.com"
                request.Accept = "*/*"
                request.Headers.Add("Accept-Language", "en-US,en;q=0.9,hi;q=0.8,ko;q=0.7")
                request.Headers.Add("Authorization", String.Format("enctoken {0}", Me.ParentController.APIConnection.ENCToken))
                request.Referer = "https://kite.zerodha.com/static/build/chart.html?v=2.4.0"
                request.Headers.Add("sec-fetch-mode", "cors")
                request.Headers.Add("sec-fetch-site", "same-origin")
                request.KeepAlive = True

                Using sr = New StreamReader(request.GetResponseAsync().Result.GetResponseStream)
                    Dim jsonString = Await sr.ReadToEndAsync.ConfigureAwait(False)
                    Dim retDictionary As Dictionary(Of String, Object) = StringManipulation.JsonDeserialize(jsonString)

                    Return retDictionary
                End Using
            Catch ex As Exception
                Throw ex
            End Try
        End Function

        Protected Overrides Async Function StartPollingAsync() As Task
            'logger.Debug("{0}->StartPollingAsync, parameters:Nothing", Me.ToString)
            Try
                ServicePointManager.DefaultConnectionLimit = 10
                _stopPollRunning = False
                _isPollRunning = False
                ServicePointManager.DefaultConnectionLimit = 10000
                Dim lastTimeWhenDone As Date = Date.MinValue
                Dim nextTimeToDo As Date = Date.MinValue
                Dim apiConnectionBeingUsed As ZerodhaConnection = Me.ParentController.APIConnection
                While True
                    If _stopPollRunning Then
                        Exit While
                    End If
                    Dim sw As New Stopwatch
                    _isPollRunning = True
                    _cts.Token.ThrowIfCancellationRequested()
                    lastTimeWhenDone = Now
                    If _subscribedInstruments IsNot Nothing AndAlso _subscribedInstruments.Count > 0 Then
                        Dim tasks = _subscribedInstruments.Select(Async Function(x)
                                                                      Try
                                                                          If x.FetchHistorical Then
                                                                              _cts.Token.ThrowIfCancellationRequested()
                                                                              Dim individualFetcher As New ZerodhaHistoricalDataFetcher(Me.ParentController,
                                                                                                              If(x.IsHistoricalCompleted, 1, _daysToGoBack),
                                                                                                              x.InstrumentIdentifier,
                                                                                                              Me._cts)
                                                                              Dim tempRet = Await individualFetcher.GetHistoricalCandleStickAsync.ConfigureAwait(False)
                                                                              If tempRet IsNot Nothing AndAlso tempRet.GetType Is GetType(Dictionary(Of String, Object)) Then
                                                                                  Dim errorMessage As String = ParentController.GetErrorResponse(tempRet)
                                                                                  If errorMessage IsNot Nothing Then
                                                                                      individualFetcher.OnFetcherError(x.InstrumentIdentifier, errorMessage)
                                                                                  Else
                                                                                      Await individualFetcher.OnFetcherCandlesAsync(x.InstrumentIdentifier, tempRet).ConfigureAwait(False)
                                                                                  End If
                                                                              Else
                                                                                  'TO DO: Uncomment this
                                                                                  Throw New ApplicationException("Fetching of historical data failed as no return detected")
                                                                              End If
                                                                          End If
                                                                      Catch ex As Exception
                                                                          'Neglect error as in the next minute, it will be run again,
                                                                          'till that time tick based candles will be used
                                                                          logger.Warn(ex)
                                                                          If Not ex.GetType Is GetType(OperationCanceledException) Then
                                                                              OnFetcherError(Me.ToString, ex.Message)
                                                                          End If
                                                                      End Try
                                                                      Return True
                                                                  End Function)
                        'OnHeartbeat("Polling historical candles")
                        logger.Debug("Polling historical candles")
                        sw.Start()
                        Await Task.WhenAll(tasks).ConfigureAwait(False)
                        sw.Stop()
                        Console.WriteLine(String.Format("Get Historical and Calling candle processor time:{0}", sw.ElapsedMilliseconds))
                        If Me.ParentController.APIConnection Is Nothing OrElse apiConnectionBeingUsed Is Nothing OrElse
                        (Me.ParentController.APIConnection IsNot Nothing AndAlso apiConnectionBeingUsed IsNot Nothing AndAlso
                        Not Me.ParentController.APIConnection.Equals(apiConnectionBeingUsed)) Then
                            Debug.WriteLine("Exiting start polling")
                            Exit While
                        End If
                    End If
                    _cts.Token.ThrowIfCancellationRequested()

                    If Utilities.Time.IsDateTimeEqualTillMinutes(lastTimeWhenDone, nextTimeToDo) Then
                        'Already done for this minute
                        lastTimeWhenDone = lastTimeWhenDone.AddMinutes(1)
                        nextTimeToDo = New Date(lastTimeWhenDone.Year, lastTimeWhenDone.Month, lastTimeWhenDone.Day, lastTimeWhenDone.Hour, lastTimeWhenDone.Minute, 5)
                    Else
                        nextTimeToDo = New Date(lastTimeWhenDone.Year, lastTimeWhenDone.Month, lastTimeWhenDone.Day, lastTimeWhenDone.Hour, lastTimeWhenDone.Minute, 5)
                    End If
                    Console.WriteLine(nextTimeToDo.ToLongTimeString)

                    While Now < nextTimeToDo
                        _cts.Token.ThrowIfCancellationRequested()
                        If _stopPollRunning Then
                            Exit While
                        End If
                        Await Task.Delay(1000, _cts.Token).ConfigureAwait(False)
                    End While
                End While
            Catch ex As Exception
                logger.Error("Instrument Identifier:{0}, error:{1}", _instrumentIdentifer, ex.ToString)
                Me.ParentController.OrphanException = ex
            Finally
                _isPollRunning = False
            End Try
        End Function

        Public Overrides Async Function SubscribeAsync(ByVal tradableInstruments As IEnumerable(Of IInstrument), ByVal maxNumberOfDays As Integer) As Task
            'logger.Debug("{0}->SubscribeAsync, instrumentIdentifiers:{1}", Me.ToString, Utils.JsonSerialize(instrumentIdentifiers))
            _cts.Token.ThrowIfCancellationRequested()
            Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
            If _subscribedInstruments Is Nothing Then
                _subscribedInstruments = New Concurrent.ConcurrentBag(Of IInstrument)
            End If
            For Each runningInstrument In tradableInstruments
                _cts.Token.ThrowIfCancellationRequested()
                Dim existingSubscribeInstruments As IEnumerable(Of IInstrument) = _subscribedInstruments.Where(Function(x)
                                                                                                                   Return x.InstrumentIdentifier = runningInstrument.InstrumentIdentifier
                                                                                                               End Function)
                If existingSubscribeInstruments IsNot Nothing AndAlso existingSubscribeInstruments.Count > 0 Then
                    If maxNumberOfDays > _daysToGoBack Then existingSubscribeInstruments.FirstOrDefault.IsHistoricalCompleted = False
                    Continue For
                End If
                _subscribedInstruments.Add(runningInstrument)
            Next
            _daysToGoBack = Math.Max(_daysToGoBack, maxNumberOfDays)
            If _subscribedInstruments Is Nothing OrElse _subscribedInstruments.Count = 0 Then
                OnHeartbeat("No instruments were subscribed for historical as they may be already subscribed")
                logger.Error("No tokens to subscribe for historical")
            Else
                OnHeartbeat(String.Format("Subscribed:{0} instruments for historical", _subscribedInstruments.Count))
            End If
        End Function

        Public Overrides Function ToString() As String
            Return Me.GetType.ToString
        End Function

        Public Overrides Sub ClearLocalUniqueSubscriptionList()
            _subscribedInstruments = Nothing
        End Sub

        Public Overrides Function IsConnected() As Boolean
            Return _isPollRunning
        End Function

        Public Overrides Async Function CloseFetcherIfConnectedAsync(ByVal forceClose As Boolean) As Task
            'Intentionally no _cts.Token.ThrowIfCancellationRequested() since we need to close the fetcher when cancellation is done
            While IsConnected()
                _stopPollRunning = True
                If forceClose Then Exit While
                Await Task.Delay(100, _cts.Token).ConfigureAwait(False)
            End While
        End Function

#Region "IDisposable Support"
        Private disposedValue As Boolean ' To detect redundant calls

        ' IDisposable
        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not disposedValue Then
                If disposing Then
                    ' TODO: dispose managed state (managed objects).
                    Dim currentZerodhaStrategyController As ZerodhaStrategyController = CType(ParentController, ZerodhaStrategyController)

                    RemoveHandler Me.FetcherCandles, AddressOf currentZerodhaStrategyController.OnFetcherCandlesAsync
                    RemoveHandler Me.FetcherError, AddressOf currentZerodhaStrategyController.OnFetcherError
                End If

                ' TODO: free unmanaged resources (unmanaged objects) and override Finalize() below.
                ' TODO: set large fields to null.
            End If
            disposedValue = True
        End Sub

        ' TODO: override Finalize() only if Dispose(disposing As Boolean) above has code to free unmanaged resources.
        'Protected Overrides Sub Finalize()
        '    ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
        '    Dispose(False)
        '    MyBase.Finalize()
        'End Sub

        ' This code added by Visual Basic to correctly implement the disposable pattern.
        Public Sub Dispose() Implements IDisposable.Dispose
            ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
            Dispose(True)
            ' TODO: uncomment the following line if Finalize() is overridden above.
            ' GC.SuppressFinalize(Me)
        End Sub
#End Region

    End Class
End Namespace