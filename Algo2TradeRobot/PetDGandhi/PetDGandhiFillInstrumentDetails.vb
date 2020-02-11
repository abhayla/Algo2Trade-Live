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

#Region "Private Class"
    Private Class StockData
        Public Symbol As String
        Public LTP As Decimal
        Public Change As Decimal
        Public Direction As String
        Public Quantity As Integer
        Public LotSize As Integer
    End Class
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

#Region "HTTP"
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
    Private Async Function GetNiftyBankStockDataAsync() As Task(Of Dictionary(Of String, Object))
        Dim ret As Dictionary(Of String, Object) = Nothing
        _cts.Token.ThrowIfCancellationRequested()
        Dim historicalDataURL As String = "https://www1.nseindia.com/live_market/dynaContent/live_watch/stock_watch/bankNiftyStockWatch.json"
        OnHeartbeat(String.Format("Fetching Nifty Bank Stock Data: {0}", historicalDataURL))
        Dim proxyToBeUsed As HttpProxy = Nothing
        Using browser As New HttpBrowser(proxyToBeUsed, Net.DecompressionMethods.GZip, New TimeSpan(0, 1, 0), _cts)
            'AddHandler browser.DocumentDownloadComplete, AddressOf OnDocumentDownloadComplete
            'AddHandler browser.Heartbeat, AddressOf OnHeartbeat
            'AddHandler browser.WaitingFor, AddressOf OnWaitingFor
            'AddHandler browser.DocumentRetryStatus, AddressOf OnDocumentRetryStatus

            browser.KeepAlive = True
            Dim headersToBeSent As New Dictionary(Of String, String)
            headersToBeSent.Add("Accept", "application/json, text/javascript, */*; q=0.01")
            headersToBeSent.Add("Accept-Encoding", "gzip, deflate, br")
            headersToBeSent.Add("Accept-Language", "en-US,en;q=0.9")
            headersToBeSent.Add("Host", "www1.nseindia.com")
            headersToBeSent.Add("Referer", "https://www1.nseindia.com/live_market/dynaContent/live_watch/equities_stock_watch.htm?cat=B")
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
                Throw New ApplicationException(String.Format("No response while getting Nifty Bank stock data for: {0}", historicalDataURL))
            End If
            If l IsNot Nothing AndAlso l.Item2 IsNot Nothing Then
                ret = l.Item2
                Try
                    logger.Debug(Utilities.Strings.JsonSerialize(ret))
                Catch ex As Exception
                    logger.Error(ex.ToString)
                End Try
            End If
            'RemoveHandler browser.DocumentDownloadComplete, AddressOf OnDocumentDownloadComplete
            'RemoveHandler browser.Heartbeat, AddressOf OnHeartbeat
            'RemoveHandler browser.WaitingFor, AddressOf OnWaitingFor
            'RemoveHandler browser.DocumentRetryStatus, AddressOf OnDocumentRetryStatus
        End Using
        Return ret
    End Function
    Private Async Function GetNiftyPSUBankStockDataAsync() As Task(Of Dictionary(Of String, Object))
        Dim ret As Dictionary(Of String, Object) = Nothing
        _cts.Token.ThrowIfCancellationRequested()
        Dim historicalDataURL As String = "https://www1.nseindia.com/live_market/dynaContent/live_watch/stock_watch/cnxPSUStockWatch.json"
        OnHeartbeat(String.Format("Fetching Nifty PSU Bank Stock Data: {0}", historicalDataURL))
        Dim proxyToBeUsed As HttpProxy = Nothing
        Using browser As New HttpBrowser(proxyToBeUsed, Net.DecompressionMethods.GZip, New TimeSpan(0, 1, 0), _cts)
            'AddHandler browser.DocumentDownloadComplete, AddressOf OnDocumentDownloadComplete
            'AddHandler browser.Heartbeat, AddressOf OnHeartbeat
            'AddHandler browser.WaitingFor, AddressOf OnWaitingFor
            'AddHandler browser.DocumentRetryStatus, AddressOf OnDocumentRetryStatus

            browser.KeepAlive = True
            Dim headersToBeSent As New Dictionary(Of String, String)
            headersToBeSent.Add("Accept", "application/json, text/javascript, */*; q=0.01")
            headersToBeSent.Add("Accept-Encoding", "gzip, deflate, br")
            headersToBeSent.Add("Accept-Language", "en-US,en;q=0.9")
            headersToBeSent.Add("Host", "www1.nseindia.com")
            headersToBeSent.Add("Referer", "https://www1.nseindia.com/live_market/dynaContent/live_watch/equities_stock_watch.htm?cat=B")
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
                Throw New ApplicationException(String.Format("No response while getting Nifty PSU Bank stock data for: {0}", historicalDataURL))
            End If
            If l IsNot Nothing AndAlso l.Item2 IsNot Nothing Then
                ret = l.Item2
                Try
                    logger.Debug(Utilities.Strings.JsonSerialize(ret))
                Catch ex As Exception
                    logger.Error(ex.ToString)
                End Try
            End If
            'RemoveHandler browser.DocumentDownloadComplete, AddressOf OnDocumentDownloadComplete
            'RemoveHandler browser.Heartbeat, AddressOf OnHeartbeat
            'RemoveHandler browser.WaitingFor, AddressOf OnWaitingFor
            'RemoveHandler browser.DocumentRetryStatus, AddressOf OnDocumentRetryStatus
        End Using
        Return ret
    End Function
    Private Async Function GetNiftyPrivateBankStockDataAsync() As Task(Of Dictionary(Of String, Object))
        Dim ret As Dictionary(Of String, Object) = Nothing
        _cts.Token.ThrowIfCancellationRequested()
        Dim historicalDataURL As String = "https://www1.nseindia.com/live_market/dynaContent/live_watch/stock_watch/niftyPvtBankStockWatch.json"
        OnHeartbeat(String.Format("Fetching Nifty Private Bank Stock Data: {0}", historicalDataURL))
        Dim proxyToBeUsed As HttpProxy = Nothing
        Using browser As New HttpBrowser(proxyToBeUsed, Net.DecompressionMethods.GZip, New TimeSpan(0, 1, 0), _cts)
            'AddHandler browser.DocumentDownloadComplete, AddressOf OnDocumentDownloadComplete
            'AddHandler browser.Heartbeat, AddressOf OnHeartbeat
            'AddHandler browser.WaitingFor, AddressOf OnWaitingFor
            'AddHandler browser.DocumentRetryStatus, AddressOf OnDocumentRetryStatus

            browser.KeepAlive = True
            Dim headersToBeSent As New Dictionary(Of String, String)
            headersToBeSent.Add("Accept", "application/json, text/javascript, */*; q=0.01")
            headersToBeSent.Add("Accept-Encoding", "gzip, deflate, br")
            headersToBeSent.Add("Accept-Language", "en-US,en;q=0.9")
            headersToBeSent.Add("Host", "www1.nseindia.com")
            headersToBeSent.Add("Referer", "https://www1.nseindia.com/live_market/dynaContent/live_watch/equities_stock_watch.htm?cat=B")
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
                Throw New ApplicationException(String.Format("No response while getting Nifty Private Bank stock data for: {0}", historicalDataURL))
            End If
            If l IsNot Nothing AndAlso l.Item2 IsNot Nothing Then
                ret = l.Item2
                Try
                    logger.Debug(Utilities.Strings.JsonSerialize(ret))
                Catch ex As Exception
                    logger.Error(ex.ToString)
                End Try
            End If
            'RemoveHandler browser.DocumentDownloadComplete, AddressOf OnDocumentDownloadComplete
            'RemoveHandler browser.Heartbeat, AddressOf OnHeartbeat
            'RemoveHandler browser.WaitingFor, AddressOf OnWaitingFor
            'RemoveHandler browser.DocumentRetryStatus, AddressOf OnDocumentRetryStatus
        End Using
        Return ret
    End Function
    Private Async Function GetNiftyFinanceStockDataAsync() As Task(Of Dictionary(Of String, Object))
        Dim ret As Dictionary(Of String, Object) = Nothing
        _cts.Token.ThrowIfCancellationRequested()
        Dim historicalDataURL As String = "https://www1.nseindia.com/live_market/dynaContent/live_watch/stock_watch/cnxFinanceStockWatch.json"
        OnHeartbeat(String.Format("Fetching Nifty Finance Bank Stock Data: {0}", historicalDataURL))
        Dim proxyToBeUsed As HttpProxy = Nothing
        Using browser As New HttpBrowser(proxyToBeUsed, Net.DecompressionMethods.GZip, New TimeSpan(0, 1, 0), _cts)
            'AddHandler browser.DocumentDownloadComplete, AddressOf OnDocumentDownloadComplete
            'AddHandler browser.Heartbeat, AddressOf OnHeartbeat
            'AddHandler browser.WaitingFor, AddressOf OnWaitingFor
            'AddHandler browser.DocumentRetryStatus, AddressOf OnDocumentRetryStatus

            browser.KeepAlive = True
            Dim headersToBeSent As New Dictionary(Of String, String)
            headersToBeSent.Add("Accept", "application/json, text/javascript, */*; q=0.01")
            headersToBeSent.Add("Accept-Encoding", "gzip, deflate, br")
            headersToBeSent.Add("Accept-Language", "en-US,en;q=0.9")
            headersToBeSent.Add("Host", "www1.nseindia.com")
            headersToBeSent.Add("Referer", "https://www1.nseindia.com/live_market/dynaContent/live_watch/equities_stock_watch.htm?cat=B")
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
                Throw New ApplicationException(String.Format("No response while getting Nifty Finance Bank stock data for: {0}", historicalDataURL))
            End If
            If l IsNot Nothing AndAlso l.Item2 IsNot Nothing Then
                ret = l.Item2
                Try
                    logger.Debug(Utilities.Strings.JsonSerialize(ret))
                Catch ex As Exception
                    logger.Error(ex.ToString)
                End Try
            End If
            'RemoveHandler browser.DocumentDownloadComplete, AddressOf OnDocumentDownloadComplete
            'RemoveHandler browser.Heartbeat, AddressOf OnHeartbeat
            'RemoveHandler browser.WaitingFor, AddressOf OnWaitingFor
            'RemoveHandler browser.DocumentRetryStatus, AddressOf OnDocumentRetryStatus
        End Using
        Return ret
    End Function
#End Region

#Region "Process JSONs"
    Private Function GetStockData(ByVal stocks As Dictionary(Of String, Object), ByVal nfoInstruments As IEnumerable(Of IInstrument), ByVal instrumentsMargin As Dictionary(Of String, Decimal)) As List(Of StockData)
        Dim ret As List(Of StockData) = Nothing
        If stocks IsNot Nothing AndAlso stocks.Count > 0 Then
            If stocks.ContainsKey("data") Then
                Dim allStocks As ArrayList = stocks("data")
                If allStocks IsNot Nothing AndAlso allStocks.Count > 0 Then
                    For Each runningStock In allStocks
                        _cts.Token.ThrowIfCancellationRequested()
                        Dim symbol As String = runningStock("symbol").ToString.Trim.ToUpper
                        Dim ltp As Decimal = runningStock("ltP")
                        Dim change As Decimal = runningStock("per")
                        Dim margin As Decimal = 0
                        If instrumentsMargin.ContainsKey(symbol) Then margin = instrumentsMargin(symbol)

                        If ltp >= 50 AndAlso ltp <= 3000 AndAlso margin >= 12 Then
                            If nfoInstruments IsNot Nothing AndAlso nfoInstruments.Count > 0 Then
                                Dim instruments As IEnumerable(Of IInstrument) = nfoInstruments.Where(Function(x)
                                                                                                          Return x.RawInstrumentName = symbol
                                                                                                      End Function)

                                If instruments IsNot Nothing AndAlso instruments.Count > 0 Then
                                    Dim stock As StockData = New StockData With {
                                                                .Symbol = symbol,
                                                                .LTP = ltp,
                                                                .Change = change,
                                                                .LotSize = instruments.FirstOrDefault.LotSize
                                                            }

                                    If ret Is Nothing Then ret = New List(Of StockData)
                                    ret.Add(stock)
                                End If
                            End If
                        End If
                    Next
                End If
            End If
        End If
        Return ret
    End Function

    Private Function GetAllStockData(ByVal nfoInstruments As IEnumerable(Of IInstrument), ByVal instrumentsMargin As Dictionary(Of String, Decimal), ParamArray ByVal stocksJsons() As Dictionary(Of String, Object)) As List(Of StockData)
        Dim ret As List(Of StockData) = Nothing
        If stocksJsons IsNot Nothing AndAlso stocksJsons.Count > 0 Then
            For ctr As Integer = 0 To stocksJsons.Count - 1
                Dim stockJson As Dictionary(Of String, Object) = stocksJsons(ctr)
                Dim stockList As List(Of StockData) = GetStockData(stockJson, nfoInstruments, instrumentsMargin)
                If stockList IsNot Nothing AndAlso stockList.Count > 0 Then
                    For Each runningStock In stockList
                        If ret Is Nothing Then
                            ret = New List(Of StockData)
                            ret.Add(runningStock)
                        Else
                            Dim availableStocks As List(Of StockData) = ret.FindAll(Function(x)
                                                                                        Return x.Symbol = runningStock.Symbol
                                                                                    End Function)
                            If availableStocks Is Nothing OrElse availableStocks.Count = 0 Then
                                ret.Add(runningStock)
                            End If
                        End If
                    Next
                End If
            Next
        End If
        Return ret
    End Function
#End Region

    Public Async Function GetInstrumentData(ByVal allInstruments As IEnumerable(Of IInstrument), ByVal bannedStock As List(Of String)) As Task
        If allInstruments IsNot Nothing AndAlso allInstruments.Count > 0 Then
            Dim nfoInstruments As IEnumerable(Of IInstrument) = allInstruments.Where(Function(x)
                                                                                         Return x.Segment = "NFO-FUT"
                                                                                     End Function)

            Try
                Dim instrumentsMargin As Dictionary(Of String, Decimal) = Await GetMISMarginDataAsync().ConfigureAwait(False)
                If instrumentsMargin IsNot Nothing AndAlso instrumentsMargin.Count > 0 Then
                    OnHeartbeat("Fetching Nifty stocks data")
                    While True
                        If _parentStrategy.ParentController.OrphanException IsNot Nothing Then
                            Throw _parentStrategy.ParentController.OrphanException
                        End If
                        _cts.Token.ThrowIfCancellationRequested()
                        If Now >= _userInputs.TradeStartTime.AddSeconds(-5) Then
                            Dim niftyBankStocks As Dictionary(Of String, Object) = Await GetNiftyBankStockDataAsync().ConfigureAwait(False)
                            _cts.Token.ThrowIfCancellationRequested()
                            Dim niftyPSUBankStocks As Dictionary(Of String, Object) = Await GetNiftyPSUBankStockDataAsync().ConfigureAwait(False)
                            _cts.Token.ThrowIfCancellationRequested()
                            Dim niftyPrivateBankStocks As Dictionary(Of String, Object) = Await GetNiftyPrivateBankStockDataAsync().ConfigureAwait(False)
                            _cts.Token.ThrowIfCancellationRequested()
                            Dim niftyFinanceStocks As Dictionary(Of String, Object) = Await GetNiftyFinanceStockDataAsync().ConfigureAwait(False)
                            _cts.Token.ThrowIfCancellationRequested()

                            OnHeartbeat("Getting stocks data")
                            Dim allStocks As List(Of StockData) = GetAllStockData(nfoInstruments, instrumentsMargin, niftyBankStocks, niftyPSUBankStocks, niftyPrivateBankStocks, niftyFinanceStocks)

                            If allStocks IsNot Nothing AndAlso allStocks.Count > 0 Then
                                Dim topGainerLooser As Tuple(Of StockData, StockData) = GetTopGainerTopLosserStock(allStocks)
                                Dim middleStock As Tuple(Of StockData, StockData) = GetMiddleStock(allStocks)

                                If _userInputs.InstrumentDetailsFilePath IsNot Nothing AndAlso
                                    File.Exists(_userInputs.InstrumentDetailsFilePath) Then
                                    File.Delete(_userInputs.InstrumentDetailsFilePath)
                                    Dim allStockData As DataTable = New DataTable
                                    allStockData.Columns.Add("Instrument Name")
                                    allStockData.Columns.Add("Quantity")
                                    allStockData.Columns.Add("Direction")

                                    If topGainerLooser IsNot Nothing Then
                                        Dim row1 As DataRow = allStockData.NewRow
                                        row1("Instrument Name") = topGainerLooser.Item1.Symbol
                                        row1("Quantity") = topGainerLooser.Item1.Quantity
                                        row1("Direction") = topGainerLooser.Item1.Direction
                                        allStockData.Rows.Add(row1)

                                        Dim row2 As DataRow = allStockData.NewRow
                                        row2("Instrument Name") = topGainerLooser.Item2.Symbol
                                        row2("Quantity") = topGainerLooser.Item2.Quantity
                                        row2("Direction") = topGainerLooser.Item2.Direction
                                        allStockData.Rows.Add(row2)
                                    End If
                                    If middleStock IsNot Nothing Then
                                        Dim row1 As DataRow = allStockData.NewRow
                                        row1("Instrument Name") = middleStock.Item1.Symbol
                                        row1("Quantity") = middleStock.Item1.Quantity
                                        row1("Direction") = middleStock.Item1.Direction
                                        allStockData.Rows.Add(row1)

                                        Dim row2 As DataRow = allStockData.NewRow
                                        row2("Instrument Name") = middleStock.Item2.Symbol
                                        row2("Quantity") = middleStock.Item2.Quantity
                                        row2("Direction") = middleStock.Item2.Direction
                                        allStockData.Rows.Add(row2)
                                    End If

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
                            Exit While
                        End If
                        Await Task.Delay(1000, _cts.Token).ConfigureAwait(False)
                    End While
                End If
            Catch ex As Exception
                logger.Error(ex.ToString)
                Throw ex
            End Try
        End If
    End Function

    Private Function GetTopGainerTopLosserStock(ByVal allStocks As List(Of StockData)) As Tuple(Of StockData, StockData)
        Dim ret As Tuple(Of StockData, StockData) = Nothing
        If allStocks IsNot Nothing AndAlso allStocks.Count >= 2 Then
            Dim orderedList As IOrderedEnumerable(Of StockData) = allStocks.OrderByDescending(Function(x)
                                                                                                  Return x.Change
                                                                                              End Function)
            Dim topGainerStock As StockData = orderedList.FirstOrDefault
            Dim topLooserStock As StockData = orderedList.LastOrDefault
            If topGainerStock.LTP > topLooserStock.LTP Then
                topGainerStock.Quantity = topGainerStock.LotSize
                topLooserStock.Quantity = Math.Ceiling(topGainerStock.LotSize * (topGainerStock.LTP / topLooserStock.LTP))
            Else
                topLooserStock.Quantity = topLooserStock.LotSize
                topGainerStock.Quantity = Math.Ceiling(topLooserStock.LotSize * (topLooserStock.LTP / topGainerStock.LTP))
            End If
            topGainerStock.Direction = "SELL"
            topLooserStock.Direction = "BUY"

            ret = New Tuple(Of StockData, StockData)(topGainerStock, topLooserStock)
        End If
        Return ret
    End Function

    Private Function GetMiddleStock(ByVal allStocks As List(Of StockData)) As Tuple(Of StockData, StockData)
        Dim ret As Tuple(Of StockData, StockData) = Nothing
        If allStocks IsNot Nothing AndAlso allStocks.Count >= 4 Then
            Dim gainerStock As StockData = Nothing
            Dim looserStock As StockData = Nothing

            Dim ctr As Integer = 0
            Dim middle As Integer = Math.Ceiling(allStocks.Count / 2)
            For Each runningStock In allStocks.OrderByDescending(Function(x)
                                                                     Return x.Change
                                                                 End Function)
                ctr += 1
                If ctr = middle Then
                    gainerStock = runningStock
                ElseIf ctr = middle + 1 Then
                    looserStock = runningStock
                    Exit For
                End If
            Next

            If gainerStock IsNot Nothing AndAlso looserStock IsNot Nothing Then
                If gainerStock.LTP > looserStock.LTP Then
                    gainerStock.Quantity = gainerStock.LotSize
                    looserStock.Quantity = Math.Ceiling(gainerStock.LotSize * (gainerStock.LTP / looserStock.LTP))
                Else
                    looserStock.Quantity = looserStock.LotSize
                    gainerStock.Quantity = Math.Ceiling(looserStock.LotSize * (looserStock.LTP / gainerStock.LTP))
                End If
                gainerStock.Direction = "BUY"
                looserStock.Direction = "SELL"
            End If
            ret = New Tuple(Of StockData, StockData)(gainerStock, looserStock)
        End If
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
