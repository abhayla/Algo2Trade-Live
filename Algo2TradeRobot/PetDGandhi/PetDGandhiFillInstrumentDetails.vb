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

    Private Async Function GetBankNiftyStockDataAsync() As Task(Of Dictionary(Of String, Object))
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
            Dim nfoInstruments As IEnumerable(Of IInstrument) = allInstruments.Where(Function(x)
                                                                                         Return x.Segment = "NFO-FUT"
                                                                                     End Function)

            Try
                Dim instrumentsMargin As Dictionary(Of String, Decimal) = Await GetMISMarginDataAsync().ConfigureAwait(False)
                If instrumentsMargin IsNot Nothing AndAlso instrumentsMargin.Count > 0 Then
                    While True
                        If Now >= _userInputs.TradeStartTime Then
                            Dim bankNiftyStocks As Dictionary(Of String, Object) = Await GetBankNiftyStockDataAsync().ConfigureAwait(False)
                            Dim allBankStocks As List(Of StockData) = Nothing
                            If bankNiftyStocks IsNot Nothing AndAlso bankNiftyStocks.Count > 0 Then
                                If bankNiftyStocks.ContainsKey("data") Then
                                    Dim allGainers As ArrayList = bankNiftyStocks("data")
                                    If allGainers IsNot Nothing AndAlso allGainers.Count > 0 Then
                                        For Each runningGainer In allGainers
                                            Dim symbol As String = runningGainer("symbol").ToString.Trim.ToUpper
                                            Dim ltp As Decimal = runningGainer("ltP")
                                            Dim change As Decimal = runningGainer("ptsC")

                                            Dim stock As StockData = New StockData With {
                                                .Symbol = symbol,
                                                .LTP = ltp,
                                                .Change = change
                                            }

                                            If allBankStocks Is Nothing Then allBankStocks = New List(Of StockData)
                                            allBankStocks.Add(stock)
                                        Next
                                    End If
                                End If
                            End If
                            If allBankStocks IsNot Nothing AndAlso allBankStocks.Count > 0 Then
                                Dim topGainerStock As StockData = Nothing
                                Dim topLooserStock As StockData = Nothing
                                For Each runningStock In allBankStocks.OrderByDescending(Function(x)
                                                                                             Return x.Change
                                                                                         End Function)
                                    If runningStock.Symbol IsNot Nothing AndAlso
                                       (bannedStock Is Nothing OrElse bannedStock IsNot Nothing AndAlso Not bannedStock.Contains(runningStock.Symbol)) Then
                                        Dim margin As Decimal = 0
                                        If instrumentsMargin.ContainsKey(runningStock.Symbol) Then margin = instrumentsMargin(runningStock.Symbol)
                                        If runningStock.LTP >= 100 AndAlso runningStock.LTP <= 3000 AndAlso margin >= 12 Then
                                            topGainerStock = runningStock
                                            topGainerStock.Direction = "SELL"
                                            Exit For
                                        End If
                                    End If
                                Next
                                If topGainerStock IsNot Nothing Then
                                    For Each runningStock In allBankStocks.OrderBy(Function(x)
                                                                                       Return x.Change
                                                                                   End Function)
                                        If runningStock.Symbol IsNot Nothing AndAlso
                                           (bannedStock Is Nothing OrElse bannedStock IsNot Nothing AndAlso Not bannedStock.Contains(runningStock.Symbol)) Then
                                            Dim margin As Decimal = 0
                                            If instrumentsMargin.ContainsKey(runningStock.Symbol) Then margin = instrumentsMargin(runningStock.Symbol)
                                            If runningStock.LTP >= 100 AndAlso runningStock.LTP <= 3000 AndAlso margin >= 12 Then
                                                topLooserStock = runningStock
                                                topLooserStock.Direction = "BUY"
                                                Exit For
                                            End If
                                        End If
                                    Next
                                End If
                                If topGainerStock IsNot Nothing AndAlso topLooserStock IsNot Nothing AndAlso
                                    topGainerStock.Symbol.ToUpper <> topLooserStock.Symbol.ToUpper Then
                                    Dim higherPriceStock As StockData = topGainerStock
                                    Dim lowerPriceStock As StockData = topLooserStock
                                    If topGainerStock.LTP < topLooserStock.LTP Then
                                        higherPriceStock = topLooserStock
                                        lowerPriceStock = topGainerStock
                                    End If

                                    Dim instruments As IEnumerable(Of IInstrument) = nfoInstruments.Where(Function(x)
                                                                                                              Return x.RawInstrumentName = higherPriceStock.Symbol
                                                                                                          End Function)
                                    If instruments IsNot Nothing AndAlso instruments.Count > 0 Then
                                        Dim higherPriceQty As Integer = instruments.FirstOrDefault.LotSize
                                        Dim lowerPriceQty As Integer = Math.Ceiling(higherPriceQty * (higherPriceStock.LTP / lowerPriceStock.LTP))

                                        If _userInputs.InstrumentDetailsFilePath IsNot Nothing AndAlso
                                            File.Exists(_userInputs.InstrumentDetailsFilePath) Then
                                            File.Delete(_userInputs.InstrumentDetailsFilePath)
                                            Dim allStockData As DataTable = Nothing
                                            allStockData = New DataTable
                                            allStockData.Columns.Add("Instrument Name")
                                            allStockData.Columns.Add("Quantity")
                                            allStockData.Columns.Add("Direction")

                                            Dim row1 As DataRow = allStockData.NewRow
                                            row1("Instrument Name") = higherPriceStock.Symbol
                                            row1("Quantity") = higherPriceQty
                                            row1("Direction") = higherPriceStock.Direction
                                            allStockData.Rows.Add(row1)

                                            Dim row2 As DataRow = allStockData.NewRow
                                            row2("Instrument Name") = lowerPriceStock.Symbol
                                            row2("Quantity") = lowerPriceQty
                                            row2("Direction") = lowerPriceStock.Direction
                                            allStockData.Rows.Add(row2)

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
                                Else
                                    Throw New ApplicationException("Unable to get 2 stock for trading")
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

    Private Class StockData
        Public Symbol As String
        Public LTP As Decimal
        Public Change As Decimal
        Public Direction As String
    End Class

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
