Imports System.IO
Imports System.Threading
Imports Algo2TradeCore.Controller
Imports Algo2TradeCore.Entities
Imports Algo2TradeCore.Strategies
Imports NLog

Public Class AmiSignalStrategy
    Inherits Strategy
#Region "Logging and Status Progress"
    Public Shared Shadows logger As Logger = LogManager.GetCurrentClassLogger
#End Region

    Public Sub New(ByVal associatedParentController As APIStrategyController,
                   ByVal strategyIdentifier As String,
                   ByVal userSettings As AmiSignalUserInputs,
                   ByVal maxNumberOfDaysForHistoricalFetch As Integer,
                   ByVal canceller As CancellationTokenSource)
        MyBase.New(associatedParentController, strategyIdentifier, False, userSettings, maxNumberOfDaysForHistoricalFetch, canceller)
        Me.ExitAllTrades = False
        'Though the TradableStrategyInstruments is being populated from inside by newing it,
        'lets also initiatilize here so that after creation of the strategy and before populating strategy instruments,
        'the fron end grid can bind to this created TradableStrategyInstruments which will be empty
        'TradableStrategyInstruments = New List(Of StrategyInstrument)
    End Sub
    Public Overrides Async Function CreateTradableStrategyInstrumentsAsync(ByVal allInstruments As IEnumerable(Of IInstrument)) As Task(Of Boolean)
        If allInstruments IsNot Nothing AndAlso allInstruments.Count > 0 Then
            logger.Debug("CreateTradableStrategyInstrumentsAsync, allInstruments.Count:{0}", allInstruments.Count)
        Else
            logger.Debug("CreateTradableStrategyInstrumentsAsync, allInstruments.Count:Nothing or 0")
        End If
        _cts.Token.ThrowIfCancellationRequested()
        Dim ret As Boolean = False
        Dim retTradableInstrumentsAsPerStrategy As List(Of IInstrument) = Nothing
        Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
        logger.Debug("Starting to fill strategy specific instruments, strategy:{0}", Me.ToString)
        If allInstruments IsNot Nothing AndAlso allInstruments.Count > 0 Then
            'Get AmiSignal Strategy Instruments
            Dim amiUserInputs As AmiSignalUserInputs = CType(UserSettings, AmiSignalUserInputs)
            If amiUserInputs.InstrumentsData IsNot Nothing AndAlso amiUserInputs.InstrumentsData.Count > 0 Then
                Dim dummyAllInstruments As List(Of IInstrument) = allInstruments.ToList
                Dim notAvailableInstruments As List(Of String) = Nothing
                For Each instrument In amiUserInputs.InstrumentsData
                    _cts.Token.ThrowIfCancellationRequested()
                    Dim runningTradableInstrument As IInstrument = dummyAllInstruments.Find(Function(x)
                                                                                                Return x.TradingSymbol.ToUpper = instrument.Value.InstrumentName.ToUpper
                                                                                            End Function)
                    _cts.Token.ThrowIfCancellationRequested()
                    ret = True
                    If retTradableInstrumentsAsPerStrategy Is Nothing Then retTradableInstrumentsAsPerStrategy = New List(Of IInstrument)
                    If runningTradableInstrument IsNot Nothing Then
                        retTradableInstrumentsAsPerStrategy.Add(runningTradableInstrument)
                    Else
                        If notAvailableInstruments Is Nothing Then notAvailableInstruments = New List(Of String)
                        notAvailableInstruments.Add(instrument.Value.InstrumentName.ToUpper)
                    End If
                Next
                If notAvailableInstruments IsNot Nothing AndAlso notAvailableInstruments.Count > 0 Then
                    Dim msg As String = Nothing
                    For Each runningNotAvailableInstrument In notAvailableInstruments
                        msg = String.Format("{0}, {1}", runningNotAvailableInstrument, msg)
                    Next
                    OnHeartbeat(String.Format("{0}{1} {2} not available at Zerodha list", msg, If(notAvailableInstruments.Count = 1, "Instrument", "Instruments"), If(notAvailableInstruments.Count = 1, "is", "are")))
                End If
                TradableInstrumentsAsPerStrategy = retTradableInstrumentsAsPerStrategy
            End If
        End If
        If retTradableInstrumentsAsPerStrategy IsNot Nothing AndAlso retTradableInstrumentsAsPerStrategy.Count > 0 Then
            'Now create the strategy tradable instruments
            Dim retTradableStrategyInstruments As List(Of AmiSignalStrategyInstrument) = Nothing
            logger.Debug("Creating strategy tradable instruments, _tradableInstruments.count:{0}", retTradableInstrumentsAsPerStrategy.Count)
            'Remove the old handlers from the previous strategyinstruments collection
            If TradableStrategyInstruments IsNot Nothing AndAlso TradableStrategyInstruments.Count > 0 Then
                For Each runningTradableStrategyInstruments In TradableStrategyInstruments
                    RemoveHandler runningTradableStrategyInstruments.HeartbeatEx, AddressOf OnHeartbeatEx
                    RemoveHandler runningTradableStrategyInstruments.WaitingForEx, AddressOf OnWaitingForEx
                    RemoveHandler runningTradableStrategyInstruments.DocumentRetryStatusEx, AddressOf OnDocumentRetryStatusEx
                    RemoveHandler runningTradableStrategyInstruments.DocumentDownloadCompleteEx, AddressOf OnDocumentDownloadCompleteEx
                Next
                TradableStrategyInstruments = Nothing
            End If

            'Now create the fresh handlers
            For Each runningTradableInstrument In retTradableInstrumentsAsPerStrategy
                _cts.Token.ThrowIfCancellationRequested()
                If retTradableStrategyInstruments Is Nothing Then retTradableStrategyInstruments = New List(Of AmiSignalStrategyInstrument)
                Dim runningTradableStrategyInstrument As New AmiSignalStrategyInstrument(runningTradableInstrument, Me, False, _cts)
                AddHandler runningTradableStrategyInstrument.HeartbeatEx, AddressOf OnHeartbeatEx
                AddHandler runningTradableStrategyInstrument.WaitingForEx, AddressOf OnWaitingForEx
                AddHandler runningTradableStrategyInstrument.DocumentRetryStatusEx, AddressOf OnDocumentRetryStatusEx
                AddHandler runningTradableStrategyInstrument.DocumentDownloadCompleteEx, AddressOf OnDocumentDownloadCompleteEx

                retTradableStrategyInstruments.Add(runningTradableStrategyInstrument)
                'If runningTradableInstrument.FirstLevelConsumers Is Nothing Then runningTradableInstrument.FirstLevelConsumers = New List(Of StrategyInstrument)
                'runningTradableInstrument.FirstLevelConsumers.Add(runningTradableStrategyInstrument)

                'Fill current day signal collections
                If File.Exists(runningTradableStrategyInstrument.EntrySignalFileName) Then runningTradableStrategyInstrument.EntrySignals = Utilities.Strings.DeserializeToCollection(Of Concurrent.ConcurrentDictionary(Of String, AmiSignalStrategyInstrument.AmiSignal))(runningTradableStrategyInstrument.EntrySignalFileName)
                If File.Exists(runningTradableStrategyInstrument.TargetSignalFileName) Then runningTradableStrategyInstrument.TargetSignals = Utilities.Strings.DeserializeToCollection(Of Concurrent.ConcurrentDictionary(Of String, AmiSignalStrategyInstrument.AmiSignal))(runningTradableStrategyInstrument.TargetSignalFileName)
                If File.Exists(runningTradableStrategyInstrument.StoplossSignalFileName) Then runningTradableStrategyInstrument.StoplossSignals = Utilities.Strings.DeserializeToCollection(Of Concurrent.ConcurrentDictionary(Of String, AmiSignalStrategyInstrument.AmiSignal))(runningTradableStrategyInstrument.StoplossSignalFileName)

                'Previous Day clean up
                Try
                    Dim todayDate As String = Now.ToString("yy_MM_dd")
                    For Each runningFile In Directory.GetFiles(My.Application.Info.DirectoryPath, "*.EntrySignal.a2t")
                        If Not runningFile.Contains(todayDate) Then File.Delete(runningFile)
                    Next
                    For Each runningFile In Directory.GetFiles(My.Application.Info.DirectoryPath, "*.TargetSignal.a2t")
                        If Not runningFile.Contains(todayDate) Then File.Delete(runningFile)
                    Next
                    For Each runningFile In Directory.GetFiles(My.Application.Info.DirectoryPath, "*.StoplossSignal.a2t")
                        If Not runningFile.Contains(todayDate) Then File.Delete(runningFile)
                    Next
                Catch ex As Exception
                    logger.Error(ex)
                End Try
            Next
            TradableStrategyInstruments = retTradableStrategyInstruments
        Else
            Throw New ApplicationException(String.Format("Cannot run this strategy as no strategy instruments could be created from the tradable instruments, stratgey:{0}", Me.ToString))
        End If

        Return ret
    End Function

    Public Overrides Async Function MonitorAsync() As Task
        Dim lastException As Exception = Nothing

        Try
            _cts.Token.ThrowIfCancellationRequested()
            Dim tasks As New List(Of Task)()
            For Each tradableStrategyInstrument As AmiSignalStrategyInstrument In TradableStrategyInstruments
                _cts.Token.ThrowIfCancellationRequested()
                tasks.Add(Task.Run(AddressOf tradableStrategyInstrument.MonitorAsync, _cts.Token))
            Next
            tasks.Add(Task.Run(AddressOf MonitorAmiBrokerAsync, _cts.Token))
            tasks.Add(Task.Run(AddressOf ForceExitAllTradesAsync, _cts.Token))
            Await Task.WhenAll(tasks).ConfigureAwait(False)
        Catch ex As Exception
            lastException = ex
            logger.Error(ex)
        End Try
        If lastException IsNot Nothing Then
            Await ParentController.CloseTickerIfConnectedAsync().ConfigureAwait(False)
            Await ParentController.CloseFetcherIfConnectedAsync(False).ConfigureAwait(False)
            Await ParentController.CloseCollectorIfConnectedAsync(False).ConfigureAwait(False)
            Throw lastException
        End If
    End Function

    Public Async Function MonitorAmiBrokerAsync() As Task
        'logger.Debug("MonitorAmiBrokerAsync, parameters:Nothing")
        Try
            If Me.ParentController.OrphanException IsNot Nothing Then
                Throw Me.ParentController.OrphanException
            End If
            _cts.Token.ThrowIfCancellationRequested()
            Dim serverIP As Net.IPAddress = Net.IPAddress.Loopback
            Dim serverPort As Integer = 64555
            Dim server As Net.Sockets.TcpListener = New Net.Sockets.TcpListener(serverIP, serverPort)
            Dim client As Net.Sockets.TcpClient = Nothing
            Dim clientData As IO.StreamReader = Nothing
            server.Start()
            While True
                Try
                    If Me.ParentController.OrphanException IsNot Nothing Then
                        Throw Me.ParentController.OrphanException
                    End If
                    _cts.Token.ThrowIfCancellationRequested()
                    If server.Pending Then
                        client = server.AcceptTcpClient
                        clientData = New IO.StreamReader(client.GetStream)
                        Dim signalsString As String = clientData.ReadLine()
                        If signalsString IsNot Nothing Then
                            Dim signalsArr() As String = signalsString.Trim.Split("~")
                            If signalsArr IsNot Nothing AndAlso signalsArr.Count > 0 Then
                                Dim lastUsedStrategyInstrument As AmiSignalStrategyInstrument = Nothing
                                Dim uniqueIdentifier As String = Guid.NewGuid.ToString
                                If signalsArr.Count = 3 Then
                                    For Each signal In signalsArr
                                        signal = signal.Trim
                                        signal = String.Format("{0} {1}", uniqueIdentifier, signal)
                                        lastUsedStrategyInstrument = Await PopulateExternalSignalAsync(signal).ConfigureAwait(False)
                                    Next
                                Else
                                    logger.Error("All signals are not there. So will not execute anyone. Given Signal:{0}", signalsString)
                                End If
                                If lastUsedStrategyInstrument IsNot Nothing AndAlso uniqueIdentifier IsNot Nothing Then
                                    While True
                                        Dim overallUsedFlag As Boolean = True
                                        If lastUsedStrategyInstrument.EntrySignals IsNot Nothing AndAlso
                                            lastUsedStrategyInstrument.EntrySignals.Count > 0 AndAlso
                                            lastUsedStrategyInstrument.EntrySignals.ContainsKey(uniqueIdentifier) Then
                                            overallUsedFlag = overallUsedFlag And lastUsedStrategyInstrument.EntrySignals(uniqueIdentifier).Used
                                        End If
                                        If lastUsedStrategyInstrument.TargetSignals IsNot Nothing AndAlso
                                            lastUsedStrategyInstrument.TargetSignals.Count > 0 AndAlso
                                            lastUsedStrategyInstrument.TargetSignals.ContainsKey(uniqueIdentifier) Then
                                            overallUsedFlag = overallUsedFlag And lastUsedStrategyInstrument.TargetSignals(uniqueIdentifier).Used
                                        End If
                                        If lastUsedStrategyInstrument.StoplossSignals IsNot Nothing AndAlso
                                            lastUsedStrategyInstrument.StoplossSignals.Count > 0 AndAlso
                                            lastUsedStrategyInstrument.StoplossSignals.ContainsKey(uniqueIdentifier) Then
                                            overallUsedFlag = overallUsedFlag And lastUsedStrategyInstrument.StoplossSignals(uniqueIdentifier).Used
                                        End If
                                        If overallUsedFlag Then
                                            Exit While
                                        Else
                                            Await Task.Delay(500, _cts.Token).ConfigureAwait(False)
                                        End If
                                    End While
                                End If
                            End If
                        End If
                    End If
                Catch cex As OperationCanceledException
                    logger.Error(cex)
                    Me.ParentController.OrphanException = cex
                    Exit While
                Catch iex As Exception
                    logger.Error("Strategy:{0}, error:{1}", Me.ToString, iex.ToString)
                    If server IsNot Nothing Then server.Stop()
                    server = Nothing
                    server = New Net.Sockets.TcpListener(serverIP, serverPort)
                    server.Start()
                End Try
                Await Task.Delay(100, _cts.Token).ConfigureAwait(False)
            End While
        Catch ex As Exception
            'To log exceptions getting created from this function as the bubble up of the exception
            'will anyways happen to Strategy.MonitorAsync but it will not be shown until all tasks exit
            logger.Error("Strategy:{0}, error:{1}", Me.ToString, ex.ToString)
            Throw ex
        End Try
    End Function

    Public Overrides Function ToString() As String
        Return Me.GetType().Name
    End Function

    Protected Overrides Function IsTriggerReceivedForExitAllOrders() As Tuple(Of Boolean, String)
        Dim ret As Tuple(Of Boolean, String) = Nothing
        Dim currentTime As Date = Now
        If currentTime >= Me.UserSettings.EODExitTime Then
            ret = New Tuple(Of Boolean, String)(True, "EOD Exit")
        End If
        Return ret
    End Function

    Private Async Function PopulateExternalSignalAsync(ByVal signal As String) As Task(Of AmiSignalStrategyInstrument)
        logger.Debug("PopulateExternalSignalAsync, parameters:{0}", signal)
        Dim ret As AmiSignalStrategyInstrument = Nothing
        Try
            Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
            If signal IsNot Nothing AndAlso Me.TradableStrategyInstruments IsNot Nothing AndAlso Me.TradableStrategyInstruments.Count > 0 Then
                Dim signalarr() As String = signal.Split(" ")

                'Signal format
                'Entry ADANIPORTS-I Buy MKT 0 200
                'StopLoss ADANIPORTS-I Sell SL-M 390 200
                'Target ADANIPORTS-I Sell LIMIT 392 200
                'Entry ADANIPORTS-I Short MKT 0 200
                'StopLoss ADANIPORTS-I Cover SL-M 393 200
                'Target ADANIPORTS-I Cover LIMIT 391 200

                If signalarr.Count > 6 Then
                    logger.Error(String.Format("Invalid Signal Details. {0}", signal))
                    Exit Function
                End If
                Dim amiUserInputs As AmiSignalUserInputs = CType(UserSettings, AmiSignalUserInputs)
                If amiUserInputs.InstrumentsData.ContainsKey(signalarr(1).ToUpper) Then
                    Dim runningStrategyInstruments As IEnumerable(Of StrategyInstrument) = Me.TradableStrategyInstruments.Where(Function(x)
                                                                                                                                    Return x.TradableInstrument.TradingSymbol.ToUpper = amiUserInputs.InstrumentsData(signalarr(1).ToUpper).InstrumentName.ToUpper
                                                                                                                                End Function)
                    If runningStrategyInstruments IsNot Nothing AndAlso runningStrategyInstruments.Count > 0 Then
                        Await CType(runningStrategyInstruments.FirstOrDefault, AmiSignalStrategyInstrument).PopulateExternalSignalAsync(signal).ConfigureAwait(False)
                        ret = CType(runningStrategyInstruments.FirstOrDefault, AmiSignalStrategyInstrument)
                    End If
                Else
                    logger.Error(String.Format("Instrument is not available in the given list. {0}", signal))
                End If
            End If
        Catch ex As Exception
            logger.Error(ex)
        End Try
        Return ret
    End Function

    Public Function GetNumberOfLogicalActiveInstruments() As Integer
        Dim instrumentCount As Integer = 0
        If TradableStrategyInstruments IsNot Nothing AndAlso TradableStrategyInstruments.Count > 0 Then
            For Each runningStrategyInstrument As AmiSignalStrategyInstrument In TradableStrategyInstruments
                If runningStrategyInstrument.IsLogicalActiveInstrument() Then
                    instrumentCount += 1
                End If
            Next
        End If
        Return instrumentCount
    End Function

End Class
