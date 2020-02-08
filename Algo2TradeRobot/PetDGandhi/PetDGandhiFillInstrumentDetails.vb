Imports System.IO
Imports System.Threading
Imports Utilities.DAL
Imports Algo2TradeCore.Entities
Imports Utilities.Network
Imports System.Net.Http
Imports NLog
Imports HtmlAgilityPack


Public Class PetDGandhiFillInstrumentDetails
    Implements IDisposable

#Region "Logging and Status Progress"
    Public Shared Shadows logger As Logger = LogManager.GetCurrentClassLogger
#End Region

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

    Private _cts As CancellationTokenSource
    Private ReadOnly _parentStrategy As PetDGandhiStrategy
    Private ReadOnly _userInputs As PetDGandhiUserInputs
    Private ReadOnly _tradingDay As Date = Date.MinValue
    Public Sub New(ByVal canceller As CancellationTokenSource, ByVal parentStrategy As PetDGandhiStrategy)
        _cts = canceller
        _parentStrategy = parentStrategy
        _userInputs = _parentStrategy.UserSettings
        _tradingDay = Now
    End Sub

    Private Async Function GetTopGainerDataAsync() As Task(Of Dictionary(Of String, Object))
        Dim ret As Dictionary(Of String, Object) = Nothing
        _cts.Token.ThrowIfCancellationRequested()
        Dim historicalDataURL As String = "https://www1.nseindia.com/live_market/dynaContent/live_analysis/gainers/fnoGainers1.json"
        OnHeartbeat(String.Format("Fetching top gainer Data: {0}", historicalDataURL))
        Dim proxyToBeUsed As HttpProxy = Nothing
        Using browser As New HttpBrowser(proxyToBeUsed, Net.DecompressionMethods.GZip, New TimeSpan(0, 1, 0), _cts)
            'AddHandler browser.DocumentDownloadComplete, AddressOf OnDocumentDownloadComplete
            'AddHandler browser.Heartbeat, AddressOf OnHeartbeat
            'AddHandler browser.WaitingFor, AddressOf OnWaitingFor
            'AddHandler browser.DocumentRetryStatus, AddressOf OnDocumentRetryStatus

            browser.KeepAlive = True
            Dim headersToBeSent As New Dictionary(Of String, String)
            headersToBeSent.Add("Accept", "*/*")
            headersToBeSent.Add("Accept-Encoding", "gzip, deflate, br")
            headersToBeSent.Add("Accept-Language", "en-US,en;q=0.9")
            headersToBeSent.Add("Host", "www1.nseindia.com")
            headersToBeSent.Add("Referer", "https://www1.nseindia.com/live_market/dynaContent/live_analysis/top_gainers_losers.htm?cat=G")
            headersToBeSent.Add("Sec-Fetch-Mode", "cors")
            headersToBeSent.Add("Sec-Fetch-Site", "same-origin")
            headersToBeSent.Add("X-Requested-With", "XMLHttpRequest")

            'Get to the landing page first
            Dim l As Tuple(Of Uri, Object) = Await browser.NonPOSTRequestAsync(historicalDataURL,
                                                                                HttpMethod.Get,
                                                                                Nothing,
                                                                                False,
                                                                                headersToBeSent,
                                                                                True,
                                                                                "application/json").ConfigureAwait(False)
            If l Is Nothing OrElse l.Item2 Is Nothing Then
                Throw New ApplicationException(String.Format("No response while getting top gainer data for: {0}", historicalDataURL))
            End If
            If l IsNot Nothing AndAlso l.Item2 IsNot Nothing Then
                ret = l.Item2
            End If
            'RemoveHandler browser.DocumentDownloadComplete, AddressOf OnDocumentDownloadComplete
            'RemoveHandler browser.Heartbeat, AddressOf OnHeartbeat
            'RemoveHandler browser.WaitingFor, AddressOf OnWaitingFor
            'RemoveHandler browser.DocumentRetryStatus, AddressOf OnDocumentRetryStatus
        End Using
        Return ret
    End Function

    Private Async Function GetTopLooserDataAsync() As Task(Of Dictionary(Of String, Object))
        Dim ret As Dictionary(Of String, Object) = Nothing
        _cts.Token.ThrowIfCancellationRequested()
        Dim historicalDataURL As String = "https://www1.nseindia.com/live_market/dynaContent/live_analysis/losers/fnoLosers1.json"
        OnHeartbeat(String.Format("Fetching top looser Data: {0}", historicalDataURL))
        Dim proxyToBeUsed As HttpProxy = Nothing
        Using browser As New HttpBrowser(proxyToBeUsed, Net.DecompressionMethods.GZip, New TimeSpan(0, 1, 0), _cts)
            'AddHandler browser.DocumentDownloadComplete, AddressOf OnDocumentDownloadComplete
            'AddHandler browser.Heartbeat, AddressOf OnHeartbeat
            'AddHandler browser.WaitingFor, AddressOf OnWaitingFor
            'AddHandler browser.DocumentRetryStatus, AddressOf OnDocumentRetryStatus

            browser.KeepAlive = True
            Dim headersToBeSent As New Dictionary(Of String, String)
            headersToBeSent.Add("Accept", "*/*")
            headersToBeSent.Add("Accept-Encoding", "gzip, deflate, br")
            headersToBeSent.Add("Accept-Language", "en-US,en;q=0.9")
            headersToBeSent.Add("Host", "www1.nseindia.com")
            headersToBeSent.Add("Referer", "https://www1.nseindia.com/live_market/dynaContent/live_analysis/top_gainers_losers.htm?cat=G")
            headersToBeSent.Add("Sec-Fetch-Mode", "cors")
            headersToBeSent.Add("Sec-Fetch-Site", "same-origin")
            headersToBeSent.Add("X-Requested-With", "XMLHttpRequest")

            'Get to the landing page first
            Dim l As Tuple(Of Uri, Object) = Await browser.NonPOSTRequestAsync(historicalDataURL,
                                                                                HttpMethod.Get,
                                                                                Nothing,
                                                                                False,
                                                                                headersToBeSent,
                                                                                True,
                                                                                "application/json").ConfigureAwait(False)
            If l Is Nothing OrElse l.Item2 Is Nothing Then
                Throw New ApplicationException(String.Format("No response while getting top looser data for: {0}", historicalDataURL))
            End If
            If l IsNot Nothing AndAlso l.Item2 IsNot Nothing Then
                ret = l.Item2
            End If
            'RemoveHandler browser.DocumentDownloadComplete, AddressOf OnDocumentDownloadComplete
            'RemoveHandler browser.Heartbeat, AddressOf OnHeartbeat
            'RemoveHandler browser.WaitingFor, AddressOf OnWaitingFor
            'RemoveHandler browser.DocumentRetryStatus, AddressOf OnDocumentRetryStatus
        End Using
        Return ret
    End Function

    Public Async Function GetInstrumentData(ByVal allInstruments As IEnumerable(Of IInstrument), ByVal bannedStock As List(Of String)) As Task
        If allInstruments IsNot Nothing AndAlso allInstruments.Count > 0 Then
            Try
                Dim instrumentsMargin As Dictionary(Of String, Decimal) = Await GetMISMarginDataAsync().ConfigureAwait(False)
                If instrumentsMargin IsNot Nothing AndAlso instrumentsMargin.Count > 0 Then
                    OnHeartbeat("Fetching Top Gainer Losser Data")
                    Dim todayStockList As List(Of String) = Nothing
                    Dim topGainers As Dictionary(Of String, Object) = Await GetTopGainerDataAsync().ConfigureAwait(False)
                    Dim topLossers As Dictionary(Of String, Object) = Await GetTopLooserDataAsync().ConfigureAwait(False)

                    Dim gainerStockCount As Integer = Math.Ceiling(_userInputs.NumberOfStock / 2)
                    If topGainers IsNot Nothing AndAlso topGainers.Count > 0 Then
                        If topGainers.ContainsKey("data") Then
                            Dim allGainers As ArrayList = topGainers("data")
                            If allGainers IsNot Nothing AndAlso allGainers.Count > 0 Then
                                Dim ctr As Integer = 0
                                For Each runningGainer In allGainers
                                    Dim symbol As String = runningGainer("symbol").ToString.Trim.ToUpper
                                    Dim ltp As Decimal = runningGainer("ltp")
                                    If symbol IsNot Nothing AndAlso
                                        (bannedStock Is Nothing OrElse bannedStock IsNot Nothing AndAlso Not bannedStock.Contains(symbol)) Then
                                        Dim margin As Decimal = 0
                                        If instrumentsMargin.ContainsKey(symbol) Then margin = instrumentsMargin(symbol)
                                        If ltp >= _userInputs.MinPrice AndAlso ltp <= _userInputs.MaxPrice AndAlso margin >= _userInputs.MinMargin Then
                                            If todayStockList Is Nothing Then todayStockList = New List(Of String)
                                            todayStockList.Add(symbol)

                                            ctr += 1
                                            If ctr >= gainerStockCount Then Exit For
                                        End If
                                    End If
                                Next
                                gainerStockCount = ctr
                            End If
                        End If
                    End If

                    Dim losserStockCount As Integer = _userInputs.NumberOfStock - gainerStockCount
                    If topLossers IsNot Nothing AndAlso topLossers.Count > 0 Then
                        If topLossers.ContainsKey("data") Then
                            Dim allLoosers As ArrayList = topLossers("data")
                            If allLoosers IsNot Nothing AndAlso allLoosers.Count > 0 Then
                                Dim ctr As Integer = 0
                                For Each runningLooser In allLoosers
                                    Dim symbol As String = runningLooser("symbol").ToString.Trim.ToUpper
                                    Dim ltp As Decimal = runningLooser("ltp")
                                    If symbol IsNot Nothing AndAlso
                                        (bannedStock Is Nothing OrElse bannedStock IsNot Nothing AndAlso Not bannedStock.Contains(symbol)) Then
                                        Dim margin As Decimal = 0
                                        If instrumentsMargin.ContainsKey(symbol) Then margin = instrumentsMargin(symbol)
                                        If ltp >= _userInputs.MinPrice AndAlso ltp <= _userInputs.MaxPrice AndAlso margin >= _userInputs.MinMargin Then
                                            If todayStockList Is Nothing Then todayStockList = New List(Of String)
                                            todayStockList.Add(symbol)

                                            ctr += 1
                                            If ctr >= losserStockCount Then Exit For
                                        End If
                                    End If
                                Next
                                losserStockCount = ctr
                            End If
                        End If
                    End If

                    If todayStockList IsNot Nothing AndAlso todayStockList.Count > 0 Then
                        If _userInputs.InstrumentDetailsFilePath IsNot Nothing AndAlso
                                    File.Exists(_userInputs.InstrumentDetailsFilePath) Then
                            File.Delete(_userInputs.InstrumentDetailsFilePath)
                            Dim allStockData As DataTable = Nothing
                            allStockData = New DataTable
                            allStockData.Columns.Add("TRADING SYMBOL")
                            allStockData.Columns.Add("ATR %")

                            For Each runningStock In todayStockList
                                Dim row As DataRow = allStockData.NewRow
                                row("TRADING SYMBOL") = runningStock
                                row("ATR %") = 0
                                allStockData.Rows.Add(row)
                            Next

                            Using csv As New CSVHelper(_userInputs.InstrumentDetailsFilePath, ",", _cts)
                                _cts.Token.ThrowIfCancellationRequested()
                                csv.GetCSVFromDataTable(allStockData)
                            End Using
                            If _userInputs.InstrumentsData IsNot Nothing Then
                                _userInputs.InstrumentsData.Clear()
                                _userInputs.InstrumentsData = Nothing
                                _userInputs.FillInstrumentDetails(_userInputs.InstrumentDetailsFilePath, _cts)
                            End If
                        End If
                    End If
                End If
            Catch ex As Exception
                logger.Error(ex.ToString)
                Throw ex
            End Try
        End If
    End Function

    Private Async Function GetMISMarginDataAsync() As Task(Of Dictionary(Of String, Decimal))
        Dim ret As Dictionary(Of String, Decimal) = Nothing
        Try
            Dim dataURL As String = "https://zerodha.com/margin-calculator/Equity/"
            Dim outputResponse As HtmlDocument = Nothing
            Dim proxyToBeUsed As HttpProxy = Nothing
            OnHeartbeat(String.Format("Fetching MIS Margin Data: {0}", dataURL))
            Using browser As New HttpBrowser(proxyToBeUsed, Net.DecompressionMethods.GZip, New TimeSpan(0, 1, 0), _cts)
                'AddHandler browser.DocumentDownloadComplete, AddressOf OnDocumentDownloadComplete
                'AddHandler browser.Heartbeat, AddressOf OnHeartbeat
                'AddHandler browser.WaitingFor, AddressOf OnWaitingFor
                'AddHandler browser.DocumentRetryStatus, AddressOf OnDocumentRetryStatus

                Dim l As Tuple(Of Uri, Object) = Await browser.NonPOSTRequestAsync(dataURL,
                                                                                    HttpMethod.Get,
                                                                                    Nothing,
                                                                                    False,
                                                                                    Nothing,
                                                                                    True,
                                                                                    "text/html").ConfigureAwait(False)

                If l IsNot Nothing AndAlso l.Item2 IsNot Nothing Then
                    outputResponse = l.Item2
                End If
                'RemoveHandler browser.DocumentDownloadComplete, AddressOf OnDocumentDownloadComplete
                'RemoveHandler browser.Heartbeat, AddressOf OnHeartbeat
                'RemoveHandler browser.WaitingFor, AddressOf OnWaitingFor
                'RemoveHandler browser.DocumentRetryStatus, AddressOf OnDocumentRetryStatus
            End Using

            If outputResponse IsNot Nothing AndAlso outputResponse.DocumentNode IsNot Nothing Then
                OnHeartbeat("Extracting margin data from HTML")

                If outputResponse.DocumentNode.SelectNodes("//table[@id='table']") IsNot Nothing Then
                    For Each table As HtmlNode In outputResponse.DocumentNode.SelectNodes("//table[@id='table']")
                        _cts.Token.ThrowIfCancellationRequested()
                        If table IsNot Nothing AndAlso table.SelectNodes("tbody") IsNot Nothing Then
                            For Each tbody As HtmlNode In table.SelectNodes("tbody")
                                If tbody IsNot Nothing AndAlso tbody.SelectNodes("tr") IsNot Nothing Then
                                    For Each row As HtmlNode In tbody.SelectNodes("tr")
                                        _cts.Token.ThrowIfCancellationRequested()
                                        If row IsNot Nothing AndAlso row.SelectNodes("td") IsNot Nothing Then
                                            Dim symbol As String = CType(row.SelectNodes("td[@class='scrip']").FirstOrDefault, HtmlNode).InnerText.Trim
                                            Dim mis As String = CType(row.SelectNodes("td[@class='mis']").FirstOrDefault, HtmlNode).InnerText.Trim

                                            symbol = symbol.Replace(":EQ", "").Trim.ToUpper
                                            mis = mis.Replace("x", "").Trim
                                            If IsNumeric(mis) Then
                                                If ret Is Nothing Then ret = New Dictionary(Of String, Decimal)
                                                ret.Add(symbol, CDec(mis))
                                            End If
                                        End If
                                    Next
                                End If
                            Next
                        End If
                    Next
                End If
            End If
        Catch ex As Exception
            logger.Error(ex.ToString)
            Throw ex
        End Try
        Return ret
    End Function

#Region "IDisposable Support"
    Private disposedValue As Boolean ' To detect redundant calls

    ' IDisposable
    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then
                ' TODO: dispose managed state (managed objects).
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
