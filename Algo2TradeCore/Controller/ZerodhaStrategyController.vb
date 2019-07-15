Imports System.Collections.Specialized
Imports System.Net
Imports Utilities.ErrorHandlers
Imports System.Threading
Imports System.Web
Imports Algo2TradeCore.Entities
Imports NLog
Imports Utilities.Network
Imports KiteConnect
Imports Algo2TradeCore.Adapter
Imports Utilities
Imports System.Net.Http
Imports Algo2TradeCore.Strategies
Imports Algo2TradeCore.Adapter.APIAdapter
Imports Algo2TradeCore.Entities.UserSettings
Imports System.IO
Imports System.Collections.Concurrent

Namespace Controller
    Public Class ZerodhaStrategyController
        Inherits APIStrategyController

#Region "Logging and Status Progress"
        Public Shared Shadows logger As Logger = LogManager.GetCurrentClassLogger
#End Region

        Public Sub New(ByVal validatedUser As ZerodhaUser,
                       ByVal associatedUserInputs As ControllerUserInputs,
                       ByVal canceller As CancellationTokenSource)
            MyBase.New(validatedUser, APISource.Zerodha, associatedUserInputs, canceller)
            _LoginURL = "https://kite.trade/connect/login"
        End Sub

#Region "Login"
        Protected Overrides Function GetLoginURL() As String
            logger.Debug("GetLoginURL, parameters:Nothing")
            Return String.Format("{0}?api_key={1}&v={2}", _LoginURL, _currentUser.APIKey, _currentUser.APIVersion)
        End Function
        Public Overrides Function GetErrorResponse(ByVal response As Object) As String
            'logger.Debug("GetErrorResponse, response:{0}", Utils.JsonSerialize(response))
            _cts.Token.ThrowIfCancellationRequested()
            Dim ret As String = Nothing

            If response IsNot Nothing AndAlso
               response.GetType = GetType(Dictionary(Of String, Object)) AndAlso
               CType(response, Dictionary(Of String, Object)).ContainsKey("status") AndAlso
               CType(response, Dictionary(Of String, Object))("status") = "error" AndAlso
               CType(response, Dictionary(Of String, Object)).ContainsKey("message") Then
                ret = String.Format("Zerodha reported error, message:{0}", CType(response, Dictionary(Of String, Object))("message"))
            End If
            Return ret
        End Function
        Public Overrides Async Function LoginAsync() As Task(Of IConnection)
            logger.Debug("LoginAsync, parameters:Nothing")
            While True
                _cts.Token.ThrowIfCancellationRequested()
                Try
                    Dim requestToken As String = Nothing

                    Dim postContent As New Dictionary(Of String, String)
                    postContent.Add("user_id", _currentUser.UserId)
                    postContent.Add("password", _currentUser.Password)
                    postContent.Add("login", "")

                    HttpBrowser.KillCookies()
                    _cts.Token.ThrowIfCancellationRequested()

                    Using browser As New HttpBrowser(Nothing, DecompressionMethods.GZip, TimeSpan.FromMinutes(1), _cts)
                        AddHandler browser.DocumentDownloadComplete, AddressOf OnDocumentDownloadComplete
                        AddHandler browser.DocumentRetryStatus, AddressOf OnDocumentRetryStatus
                        AddHandler browser.Heartbeat, AddressOf OnHeartbeat
                        AddHandler browser.WaitingFor, AddressOf OnWaitingFor

                        'Keep the below headers constant for all login browser operations
                        browser.UserAgent = GetRandomUserAgent()
                        browser.KeepAlive = True

                        Dim redirectedURI As Uri = Nothing

                        'Now launch the authentication page
                        Dim headers As New Dictionary(Of String, String)
                        headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8")
                        'headers.Add("Accept-Encoding", "gzip, deflate, br")
                        headers.Add("Accept-Encoding", "*")
                        headers.Add("Accept-Language", "en-US,en;q=0.8")
                        headers.Add("Host", "kite.trade")
                        headers.Add("X-Kite-version", _currentUser.APIVersion)

                        OnHeartbeat("Opening login page")
                        logger.Debug("Opening login page, GetLoginURL:{0}, headers:{1}", GetLoginURL, Utils.JsonSerialize(headers))

                        _cts.Token.ThrowIfCancellationRequested()
                        Dim tempRet As Tuple(Of Uri, Object) = Await browser.NonPOSTRequestAsync(GetLoginURL,
                                                                                              Http.HttpMethod.Get,
                                                                                              Nothing,
                                                                                              False,
                                                                                              headers,
                                                                                              False,
                                                                                              Nothing).ConfigureAwait(False)

                        _cts.Token.ThrowIfCancellationRequested()
                        'Should be getting back the redirected URL in Item1 and the htmldocument response in Item2
                        Dim finalURLToCall As Uri = Nothing
                        If tempRet IsNot Nothing AndAlso tempRet.Item1 IsNot Nothing AndAlso tempRet.Item1.ToString.Contains("sess_id") Then
                            logger.Debug("Login page returned, sess_id string:{0}", tempRet.Item1.ToString)
                            finalURLToCall = tempRet.Item1
                            redirectedURI = tempRet.Item1

                            postContent = New Dictionary(Of String, String)
                            postContent.Add("user_id", _currentUser.UserId)
                            postContent.Add("password", _currentUser.Password)
                            'postContent.Add("login", "")

                            'Now prepare the step 1 authentication
                            headers = New Dictionary(Of String, String)
                            headers.Add("Accept", "application/json, text/plain, */*")
                            headers.Add("Accept-Language", "en-US")
                            'headers.Add("Accept-Encoding", "gzip, deflate, br")
                            headers.Add("Content-Type", "application/x-www-form-urlencoded")
                            headers.Add("Host", "kite.zerodha.com")
                            headers.Add("Origin", "https://kite.zerodha.com")
                            headers.Add("X-Kite-version", _currentUser.APIVersion)

                            tempRet = Nothing
                            OnHeartbeat("Submitting Id/pass")
                            logger.Debug("Submitting Id/pass, redirectedURI:{0}, postContent:{1}, headers:{2}", redirectedURI.ToString, Utils.JsonSerialize(postContent), Utils.JsonSerialize(headers))
                            _cts.Token.ThrowIfCancellationRequested()
                            tempRet = Await browser.POSTRequestAsync("https://kite.zerodha.com/api/login",
                                                     redirectedURI.ToString,
                                                     postContent,
                                                     False,
                                                     headers,
                                                     False).ConfigureAwait(False)
                            _cts.Token.ThrowIfCancellationRequested()
                            'Should come back as redirected url in Item1 and htmldocument in Item2
                            Dim twoFAUserId As String = Nothing
                            Dim twoFARequestId As String = Nothing
                            Dim twoFAPIN As String = Nothing

                            If tempRet IsNot Nothing AndAlso tempRet.Item2 IsNot Nothing AndAlso tempRet.Item2.GetType Is GetType(Dictionary(Of String, Object)) AndAlso
                                tempRet.Item2.containskey("status") AndAlso tempRet.Item2("status") = "success" AndAlso
                                tempRet.Item2.containskey("data") AndAlso tempRet.Item2("data").containskey("user_id") AndAlso
                                tempRet.Item2("data").containskey("request_id") Then


                                'user_id=DK4056&request_id=Ypnc3WNKh1ulM8jP5QsmZmCUdSBI8EqT0aS9uhiHYrBNgodDla1y7VhTZE8z4Ia9&twofa_value=111111
                                twoFAUserId = tempRet.Item2("data")("user_id")
                                twoFARequestId = tempRet.Item2("data")("request_id")
                                twoFAPIN = _currentUser.API2FAPin
                                If twoFAUserId IsNot Nothing AndAlso twoFARequestId IsNot Nothing Then
                                    logger.Debug("Id/pass submission returned, twoFAUserId:{0}, twoFARequestId:{1}", twoFAUserId, twoFARequestId)
                                    'Now preprate the 2 step authentication
                                    Dim stringPostContent As New Http.StringContent(String.Format("user_id={0}&request_id={1}&twofa_value={2}",
                                                                                      Uri.EscapeDataString(twoFAUserId),
                                                                                      Uri.EscapeDataString(twoFARequestId),
                                                                                      Uri.EscapeDataString(twoFAPIN)),
                                                                        Text.Encoding.UTF8, "application/x-www-form-urlencoded")

                                    headers = New Dictionary(Of String, String)
                                    headers.Add("Accept", "application/json, text/plain, */*")
                                    headers.Add("Accept-Language", "en-US,en;q=0.5")
                                    'headers.Add("Accept-Encoding", "gzip, deflate, br")
                                    headers.Add("Content-Type", "application/x-www-form-urlencoded")
                                    headers.Add("Host", "kite.zerodha.com")
                                    headers.Add("Origin", "https://kite.zerodha.com")
                                    headers.Add("X-Kite-version", _currentUser.APIVersion)

                                    tempRet = Nothing
                                    OnHeartbeat("Submitting 2FA")
                                    logger.Debug("Submitting 2FA, redirectedURI:{0}, stringPostContent:{1}, headers:{2}", redirectedURI.ToString, Await stringPostContent.ReadAsStringAsync().ConfigureAwait(False), Utils.JsonSerialize(headers))
                                    _cts.Token.ThrowIfCancellationRequested()
                                    tempRet = Await browser.POSTRequestAsync("https://kite.zerodha.com/api/twofa",
                                                                 redirectedURI.ToString,
                                                                 stringPostContent,
                                                                 False,
                                                                 headers,
                                                                 False).ConfigureAwait(False)
                                    _cts.Token.ThrowIfCancellationRequested()

                                    'Should come back as redirect url in Item1 and htmldocument response in Item2
                                    If tempRet IsNot Nothing AndAlso tempRet.Item1 IsNot Nothing AndAlso tempRet.Item1.ToString.Contains("request_token") Then
                                        redirectedURI = tempRet.Item1
                                        Dim queryStrings As NameValueCollection = HttpUtility.ParseQueryString(redirectedURI.Query)
                                        requestToken = queryStrings("request_token")
                                        logger.Debug("2FA submission returned, requestToken:{0}", requestToken)
                                        logger.Debug("Authentication complete, requestToken:{0}", requestToken)
                                    ElseIf tempRet IsNot Nothing AndAlso tempRet.Item2 IsNot Nothing AndAlso tempRet.Item2.GetType Is GetType(Dictionary(Of String, Object)) AndAlso
                                        tempRet.Item2.containskey("status") AndAlso tempRet.Item2("status") = "success" Then
                                        logger.Debug("2FA submission returned, redirection:true")

                                        headers = New Dictionary(Of String, String)
                                        headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8")
                                        headers.Add("Accept-Encoding", "gzip, deflate, br")
                                        headers.Add("Accept-Language", "en-US,en;q=0.5")
                                        headers.Add("Host", "kite.zerodha.com")
                                        headers.Add("X-Kite-version", _currentUser.APIVersion)
                                        tempRet = Nothing

                                        OnHeartbeat("Addressing redirection")
                                        logger.Debug("Redirecting, finalURLToCall:{0}, headers:{1}", finalURLToCall.ToString, Utils.JsonSerialize(headers))
                                        _cts.Token.ThrowIfCancellationRequested()
                                        tempRet = Await browser.NonPOSTRequestAsync(String.Format("{0}&skip_session=true", finalURLToCall.ToString),
                                                                        Http.HttpMethod.Get,
                                                                        finalURLToCall.ToString,
                                                                        False,
                                                                        headers,
                                                                        True,
                                                                        Nothing).ConfigureAwait(False)
                                        _cts.Token.ThrowIfCancellationRequested()
                                        If tempRet IsNot Nothing AndAlso tempRet.Item1 IsNot Nothing AndAlso tempRet.Item1.ToString.Contains("request_token") Then
                                            redirectedURI = tempRet.Item1
                                            Dim queryStrings As NameValueCollection = HttpUtility.ParseQueryString(redirectedURI.Query)
                                            requestToken = queryStrings("request_token")
                                            logger.Debug("Redirection returned, requestToken:{0}", requestToken)
                                            logger.Debug("Authentication complete, requestToken:{0}", requestToken)
                                        Else
                                            If tempRet IsNot Nothing AndAlso tempRet.Item2 IsNot Nothing AndAlso tempRet.Item2.GetType Is GetType(Dictionary(Of String, Object)) Then
                                                Throw New AuthenticationException(GetErrorResponse(tempRet.Item2),
                                                                   AuthenticationException.TypeOfException.SecondLevelFailure)
                                            Else
                                                Throw New AuthenticationException("Step 2 authentication did not produce any request_token after redirection",
                                                                   AuthenticationException.TypeOfException.SecondLevelFailure)
                                            End If
                                        End If
                                    Else
                                        If tempRet IsNot Nothing AndAlso tempRet.Item2 IsNot Nothing AndAlso tempRet.Item2.GetType Is GetType(Dictionary(Of String, Object)) Then
                                            Throw New AuthenticationException(GetErrorResponse(tempRet.Item2),
                                                                   AuthenticationException.TypeOfException.SecondLevelFailure)
                                        Else
                                            Throw New AuthenticationException("Step 2 authentication did not produce any request_token",
                                                                   AuthenticationException.TypeOfException.SecondLevelFailure)
                                        End If
                                    End If
                                Else
                                    If tempRet IsNot Nothing AndAlso tempRet.Item2 IsNot Nothing AndAlso tempRet.Item2.GetType Is GetType(Dictionary(Of String, Object)) Then
                                        Throw New AuthenticationException(GetErrorResponse(tempRet.Item2),
                                                                   AuthenticationException.TypeOfException.SecondLevelFailure)
                                    Else
                                        Throw New AuthenticationException("Step 2 authentication did not produce first or second questions",
                                                                   AuthenticationException.TypeOfException.SecondLevelFailure)
                                    End If
                                End If
                            Else
                                If tempRet IsNot Nothing AndAlso tempRet.Item2 IsNot Nothing AndAlso tempRet.Item2.GetType Is GetType(Dictionary(Of String, Object)) Then
                                    Throw New AuthenticationException(GetErrorResponse(tempRet.Item2),
                                                                   AuthenticationException.TypeOfException.FirstLevelFailure)
                                Else
                                    Throw New AuthenticationException("Step 1 authentication did not produce any questions in the response", AuthenticationException.TypeOfException.FirstLevelFailure)
                                End If
                            End If
                        Else
                            If tempRet IsNot Nothing AndAlso tempRet.Item2 IsNot Nothing AndAlso tempRet.Item2.GetType Is GetType(Dictionary(Of String, Object)) Then
                                Throw New AuthenticationException(GetErrorResponse(tempRet.Item2),
                                                                   AuthenticationException.TypeOfException.FirstLevelFailure)
                            Else
                                Throw New AuthenticationException("Step 1 authentication prepration to get to the login page failed",
                                                                   AuthenticationException.TypeOfException.FirstLevelFailure)
                            End If
                        End If
                        RemoveHandler browser.DocumentDownloadComplete, AddressOf OnDocumentDownloadComplete
                        RemoveHandler browser.DocumentRetryStatus, AddressOf OnDocumentRetryStatus
                        RemoveHandler browser.Heartbeat, AddressOf OnHeartbeat
                        RemoveHandler browser.WaitingFor, AddressOf OnWaitingFor
                    End Using
                    If requestToken IsNot Nothing Then
                        _cts.Token.ThrowIfCancellationRequested()
                        APIConnection = Await RequestAccessTokenAsync(requestToken).ConfigureAwait(False)
                        _cts.Token.ThrowIfCancellationRequested()

                        'Now open the ticker
                        If _APITicker IsNot Nothing Then
                            _APITicker.ClearLocalUniqueSubscriptionList()
                            _cts.Token.ThrowIfCancellationRequested()
                            Await _APITicker.CloseTickerIfConnectedAsync().ConfigureAwait(False)
                            _cts.Token.ThrowIfCancellationRequested()
                            RemoveHandler _APITicker.Heartbeat, AddressOf OnHeartbeat
                            RemoveHandler _APITicker.WaitingFor, AddressOf OnWaitingFor
                            RemoveHandler _APITicker.DocumentRetryStatus, AddressOf OnDocumentRetryStatus
                            RemoveHandler _APITicker.DocumentDownloadComplete, AddressOf OnDocumentDownloadComplete
                            'Else
                        End If
                        _APITicker = New ZerodhaTicker(Me, _cts)
                        'End If

                        AddHandler _APITicker.Heartbeat, AddressOf OnHeartbeat
                        AddHandler _APITicker.WaitingFor, AddressOf OnWaitingFor
                        AddHandler _APITicker.DocumentRetryStatus, AddressOf OnDocumentRetryStatus
                        AddHandler _APITicker.DocumentDownloadComplete, AddressOf OnDocumentDownloadComplete

                        _cts.Token.ThrowIfCancellationRequested()
                        Await _APITicker.ConnectTickerAsync().ConfigureAwait(False)
                        _cts.Token.ThrowIfCancellationRequested()

                        'Now open the historicaldatafetcher
                        If _APIHistoricalDataFetcher IsNot Nothing Then
                            _APIHistoricalDataFetcher.ClearLocalUniqueSubscriptionList()
                            _cts.Token.ThrowIfCancellationRequested()
                            Await _APIHistoricalDataFetcher.CloseFetcherIfConnectedAsync(True).ConfigureAwait(False)
                            _cts.Token.ThrowIfCancellationRequested()
                            RemoveHandler _APIHistoricalDataFetcher.Heartbeat, AddressOf OnHeartbeat
                            RemoveHandler _APIHistoricalDataFetcher.WaitingFor, AddressOf OnWaitingFor
                            RemoveHandler _APIHistoricalDataFetcher.DocumentRetryStatus, AddressOf OnDocumentRetryStatus
                            RemoveHandler _APIHistoricalDataFetcher.DocumentDownloadComplete, AddressOf OnDocumentDownloadComplete
                            'Else
                        End If
                        _APIHistoricalDataFetcher = New ZerodhaHistoricalDataFetcher(Me, 0, _cts)
                        'End If

                        AddHandler _APIHistoricalDataFetcher.Heartbeat, AddressOf OnHeartbeat
                        AddHandler _APIHistoricalDataFetcher.WaitingFor, AddressOf OnWaitingFor
                        AddHandler _APIHistoricalDataFetcher.DocumentRetryStatus, AddressOf OnDocumentRetryStatus
                        AddHandler _APIHistoricalDataFetcher.DocumentDownloadComplete, AddressOf OnDocumentDownloadComplete

                        _cts.Token.ThrowIfCancellationRequested()
                        Await _APIHistoricalDataFetcher.ConnectFetcherAsync().ConfigureAwait(False)
                        _cts.Token.ThrowIfCancellationRequested()
                    End If
                Catch tex As KiteConnect.TokenException
                    logger.Error(tex)
                    OnHeartbeat("Possible error while generating session, token may be invalid, retrying the whole login process")
                    Continue While
                End Try
                _cts.Token.ThrowIfCancellationRequested()
                Exit While
            End While
            Return APIConnection
        End Function
        Private Async Function RequestAccessTokenAsync(ByVal requestToken As String) As Task(Of ZerodhaConnection)
            logger.Debug("RequestAccessTokenAsync, requestToken:{0}", requestToken)

            _cts.Token.ThrowIfCancellationRequested()
            Dim ret As ZerodhaConnection = Nothing
            Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
            Dim kiteConnector As New Kite(_currentUser.APIKey, Debug:=True)

            Dim lastException As Exception = Nothing
            Dim allOKWithoutException As Boolean = False

            Using Waiter As New Waiter(_cts)
                AddHandler Waiter.Heartbeat, AddressOf OnHeartbeat
                AddHandler Waiter.WaitingFor, AddressOf OnWaitingFor

                For retryCtr = 1 To _MaxReTries
                    _cts.Token.ThrowIfCancellationRequested()
                    lastException = Nothing
                    OnHeartbeat("Generating session...")
                    logger.Debug("Generating session, command:{0}, requestToken:{1}, _currentUser.APISecret:{2}",
                                                    "GenerateSession", requestToken, _currentUser.APISecret)
                    OnDocumentRetryStatus(retryCtr, _MaxReTries)
                    Try
                        _cts.Token.ThrowIfCancellationRequested()
                        Dim user As User = kiteConnector.GenerateSession(requestToken, _currentUser.APISecret)
                        CType(_currentUser, ZerodhaUser).WrappedUser = user
                        _cts.Token.ThrowIfCancellationRequested()
                        Console.WriteLine(Utils.JsonSerialize(user))
                        logger.Debug("Processing response")

                        If user.AccessToken IsNot Nothing Then
                            kiteConnector.SetAccessToken(user.AccessToken)
                            logger.Debug("Session generated, user.AccessToken:{0}", user.AccessToken)

                            ret = New ZerodhaConnection
                            With ret
                                .ZerodhaUser = New ZerodhaUser() With {.UserId = _currentUser.UserId,
                                                                        .Password = _currentUser.Password,
                                                                        .APIKey = _currentUser.APIKey,
                                                                        .API2FAPin = _currentUser.API2FAPin,
                                                                        .APISecret = _currentUser.APISecret,
                                                                        .APIVersion = _currentUser.APIVersion,
                                                                        .WrappedUser = user}
                                .RequestToken = requestToken
                            End With
                            lastException = Nothing
                            allOKWithoutException = True
                            Exit For
                        Else
                            Throw New ApplicationException(String.Format("Generating session did not succeed, command:{0}", "GenerateSession"))
                        End If
                        _cts.Token.ThrowIfCancellationRequested()
                    Catch tex As KiteConnect.TokenException
                        logger.Error(tex)
                        lastException = tex
                        Exit For
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
            ' For handling 403 errors
            '_Kite.SetSessionExpiryHook(AddressOf OnSessionExpireAsync)
            Return ret
        End Function
        Public Async Sub OnSessionExpireAsync()
            Try
                OrphanException = Nothing
                logger.Debug("OnSessionExpireAsync, parameters:Nothing")
                '_cts.Cancel()
                _cts.Token.ThrowIfCancellationRequested()
                'Wait for the lock and if locked, then relexit immediately
                If _LoginThreads = 0 Then
                    Interlocked.Increment(_LoginThreads)
                    APIConnection = Nothing
                Else
                    Exit Sub
                End If
                OnHeartbeat("********** Need to login again **********")
                Dim tempConn As ZerodhaConnection = Nothing
                Try
                    _cts.Token.ThrowIfCancellationRequested()
                    Await Task.Delay(2000, _cts.Token).ConfigureAwait(False)
                    Dim loginMessage As String = Nothing
                    While True
                        _cts.Token.ThrowIfCancellationRequested()
                        loginMessage = Nothing
                        tempConn = Nothing
                        Try
                            OnHeartbeat("Attempting to get connection to Zerodha API")
                            _cts.Token.ThrowIfCancellationRequested()
                            tempConn = Await LoginAsync().ConfigureAwait(False)
                            _cts.Token.ThrowIfCancellationRequested()
                        Catch ex As Exception
                            loginMessage = ex.Message
                            logger.Error(ex)
                        End Try
                        If tempConn Is Nothing Then
                            If loginMessage IsNot Nothing AndAlso (loginMessage.ToUpper.Contains("password".ToUpper) OrElse loginMessage.ToUpper.Contains("api_key".ToUpper) OrElse loginMessage.ToUpper.Contains("username".ToUpper)) Then
                                'No need to retry as its a password failure
                                OnHeartbeat(String.Format("Loging process failed:{0}", loginMessage))
                                Exit While
                            Else
                                OnHeartbeat(String.Format("Login process failed after token expiry:{0} | Waiting for 10 seconds before retrying connection", loginMessage))
                                _cts.Token.ThrowIfCancellationRequested()
                                Await Task.Delay(10000, _cts.Token).ConfigureAwait(False)
                                _cts.Token.ThrowIfCancellationRequested()
                            End If
                        Else
                            'Now open the informationCollector
                            If _APIInformationCollector IsNot Nothing Then
                                _cts.Token.ThrowIfCancellationRequested()
                                Await _APIInformationCollector.CloseCollectorIfConnectedAsync(True).ConfigureAwait(False)
                                _cts.Token.ThrowIfCancellationRequested()
                                RemoveHandler _APIInformationCollector.Heartbeat, AddressOf OnHeartbeat
                                RemoveHandler _APIInformationCollector.WaitingFor, AddressOf OnWaitingFor
                                RemoveHandler _APIInformationCollector.DocumentRetryStatus, AddressOf OnDocumentRetryStatus
                                RemoveHandler _APIInformationCollector.DocumentDownloadComplete, AddressOf OnDocumentDownloadComplete
                                'Else
                            End If
                            _APIInformationCollector = New ZerodhaInformationCollector(Me, Me.UserInputs.GetInformationDelay, _cts)
                            'End If
                            AddHandler _APIInformationCollector.Heartbeat, AddressOf OnHeartbeat
                            AddHandler _APIInformationCollector.WaitingFor, AddressOf OnWaitingFor
                            AddHandler _APIInformationCollector.DocumentRetryStatus, AddressOf OnDocumentRetryStatus
                            AddHandler _APIInformationCollector.DocumentDownloadComplete, AddressOf OnDocumentDownloadComplete
                            _cts.Token.ThrowIfCancellationRequested()
                            Await _APIInformationCollector.ConnectCollectorAsync().ConfigureAwait(False)
                            _cts.Token.ThrowIfCancellationRequested()

                            OnHeartbeat("Relogin completed, checking to see if strategy instruments need to be resubscribed")

                            If _AllStrategies IsNot Nothing AndAlso _AllStrategies.Count > 0 Then
                                For Each runningStrategy In _AllStrategies
                                    _cts.Token.ThrowIfCancellationRequested()
                                    OnHeartbeatEx(String.Format("Resubscribing strategy instruments, strategy:{0}", runningStrategy.ToString), New List(Of Object) From {runningStrategy})
                                    _cts.Token.ThrowIfCancellationRequested()
                                    Await runningStrategy.SubscribeAsync(_APITicker, _APIHistoricalDataFetcher).ConfigureAwait(False)
                                    _cts.Token.ThrowIfCancellationRequested()
                                    OnSessionExpiry(runningStrategy)
                                    _cts.Token.ThrowIfCancellationRequested()
                                Next
                            End If
                            OnHeartbeat("Relogin completed with resubscriptions")
                            Exit While
                        End If
                    End While
                    If tempConn Is Nothing Then
                        If loginMessage IsNot Nothing Then
                            Throw New ApplicationException(String.Format("No connection to Zerodha API could be established | Details:{0}", loginMessage))
                        Else
                            Throw New ApplicationException("No connection to Zerodha API could be established")
                        End If
                    End If
                Finally
                    Interlocked.Decrement(_LoginThreads)
                End Try
                _cts.Token.ThrowIfCancellationRequested()
            Catch ex As Exception
                logger.Error(ex)
                OrphanException = ex
                'This should be now handled by the strategies in their monitor method
            End Try
        End Sub
#End Region

#Region "Common tasks for all strategies"
        Public Overrides Async Function PrepareToRunStrategyAsync() As Task(Of Boolean)
            logger.Debug("PrepareToRunStrategyAsync, parameters:Nothing")
            _cts.Token.ThrowIfCancellationRequested()

            Dim ret As Boolean = False
            _AllStrategies = Nothing
            _AllInstruments = Nothing
            _AllStrategyUniqueInstruments = Nothing
            _rawPayloadCreators = Nothing
            If _APIAdapter IsNot Nothing Then
                RemoveHandler _APIAdapter.Heartbeat, AddressOf OnHeartbeat
                RemoveHandler _APIAdapter.WaitingFor, AddressOf OnWaitingFor
                RemoveHandler _APIAdapter.DocumentRetryStatus, AddressOf OnDocumentRetryStatus
                RemoveHandler _APIAdapter.DocumentDownloadComplete, AddressOf OnDocumentDownloadComplete
            End If
            _APIAdapter = New ZerodhaAdapter(Me, _cts)
            AddHandler _APIAdapter.Heartbeat, AddressOf OnHeartbeat
            AddHandler _APIAdapter.WaitingFor, AddressOf OnWaitingFor
            AddHandler _APIAdapter.DocumentRetryStatus, AddressOf OnDocumentRetryStatus
            AddHandler _APIAdapter.DocumentDownloadComplete, AddressOf OnDocumentDownloadComplete

            'Now open the informationCollector
            If _APIInformationCollector IsNot Nothing Then
                _cts.Token.ThrowIfCancellationRequested()
                Await _APIInformationCollector.CloseCollectorIfConnectedAsync(True).ConfigureAwait(False)
                _cts.Token.ThrowIfCancellationRequested()
                RemoveHandler _APIInformationCollector.Heartbeat, AddressOf OnHeartbeat
                RemoveHandler _APIInformationCollector.WaitingFor, AddressOf OnWaitingFor
                RemoveHandler _APIInformationCollector.DocumentRetryStatus, AddressOf OnDocumentRetryStatus
                RemoveHandler _APIInformationCollector.DocumentDownloadComplete, AddressOf OnDocumentDownloadComplete
                'Else
            End If
            _APIInformationCollector = New ZerodhaInformationCollector(Me, Me.UserInputs.GetInformationDelay, _cts)
            'End If
            AddHandler _APIInformationCollector.Heartbeat, AddressOf OnHeartbeat
            AddHandler _APIInformationCollector.WaitingFor, AddressOf OnWaitingFor
            AddHandler _APIInformationCollector.DocumentRetryStatus, AddressOf OnDocumentRetryStatus
            AddHandler _APIInformationCollector.DocumentDownloadComplete, AddressOf OnDocumentDownloadComplete
            _cts.Token.ThrowIfCancellationRequested()
            Await _APIInformationCollector.ConnectCollectorAsync().ConfigureAwait(False)
            _cts.Token.ThrowIfCancellationRequested()

            Await FillUserStartingMargin(_userMarginFilename).ConfigureAwait(False)
            _cts.Token.ThrowIfCancellationRequested()

            Dim execCommand As ExecutionCommands = ExecutionCommands.GetInstruments
            _AllInstruments = Await ExecuteCommandAsync(execCommand, Nothing).ConfigureAwait(False)
            _cts.Token.ThrowIfCancellationRequested()
            Await FillQuantityMultiplierMapAsync().ConfigureAwait(False)
            _cts.Token.ThrowIfCancellationRequested()
            Using bannedStock As New BannedStockDataFetcher(_cts)
                AddHandler bannedStock.Heartbeat, AddressOf OnHeartbeat
                AddHandler bannedStock.DocumentDownloadComplete, AddressOf OnDocumentDownloadComplete
                AddHandler bannedStock.DocumentRetryStatus, AddressOf OnDocumentRetryStatus
                _AllBannedStock = Await bannedStock.GetBannedStocksData.ConfigureAwait(False)
            End Using

            Return _AllInstruments IsNot Nothing AndAlso _AllInstruments.Count > 0 AndAlso
                Me._currentUser.DaysStartingCapitals IsNot Nothing AndAlso Me._currentUser.DaysStartingCapitals.Count > 0
        End Function

        Protected Overrides Async Function FillQuantityMultiplierMapAsync() As Task
            Dim commodityMultiplierMap As Dictionary(Of String, Object) = Nothing
            Dim proxyToBeUsed As HttpProxy = Nothing
            Using browser As New HttpBrowser(proxyToBeUsed, Net.DecompressionMethods.GZip, New TimeSpan(0, 1, 0), _cts)
                Dim l As Tuple(Of Uri, Object) = Await browser.NonPOSTRequestAsync("https://zerodha.com/static/app.js",
                                                                                     HttpMethod.Get,
                                                                                     Nothing,
                                                                                     True,
                                                                                     Nothing,
                                                                                     False,
                                                                                     Nothing).ConfigureAwait(False)
                If l Is Nothing OrElse l.Item2 Is Nothing Then
                    Throw New ApplicationException(String.Format("No response in the additional site's historical race results landing page: {0}", "https://zerodha.com/static/app.js"))
                End If
                If l IsNot Nothing AndAlso l.Item2 IsNot Nothing Then
                    Dim jString As String = l.Item2
                    If jString IsNot Nothing Then
                        Dim map As String = Utilities.Strings.GetTextBetween("COMMODITY_MULTIPLIER_MAP=", "},", jString)
                        If map IsNot Nothing Then
                            map = map & "}"
                            commodityMultiplierMap = Utilities.Strings.JsonDeserialize(map)
                        End If
                    End If
                End If
            End Using
            If commodityMultiplierMap IsNot Nothing AndAlso commodityMultiplierMap.Count > 0 Then
                If _AllInstruments IsNot Nothing AndAlso _AllInstruments.Count > 0 Then
                    For Each instrument In _AllInstruments
                        instrument.ExchangeDetails = Me.UserInputs.ExchangeDetails(instrument.RawExchange)

                        If instrument.InstrumentType = IInstrument.TypeOfInstrument.Futures AndAlso
                            instrument.ExchangeDetails.ExchangeType = Enums.TypeOfExchage.MCX Then
                            Dim stockName As String = instrument.TradingSymbol.Remove(instrument.TradingSymbol.Count - 8)
                            If commodityMultiplierMap.ContainsKey(stockName) Then
                                instrument.QuantityMultiplier = Val(commodityMultiplierMap(stockName).ToString.Substring(0, commodityMultiplierMap(stockName).ToString.Length - 1))
                                instrument.BrokerageCategory = commodityMultiplierMap(stockName).ToString.Substring(commodityMultiplierMap(stockName).ToString.Length - 1)
                            Else
                                logger.Warn(String.Format("Commodity Multiplier Map doesn't have this MCX stock - {0}", stockName))
                            End If
                        Else
                            instrument.QuantityMultiplier = 1
                            instrument.BrokerageCategory = Nothing
                        End If
                    Next
                End If
            Else
                Throw New ApplicationException("Unable to fetch quantity multiplier")
            End If
        End Function

        ''' <summary>
        ''' This will help find all tradable instruments as per the passed strategy and then create the strategy workers for each of these instruments
        ''' </summary>
        ''' <param name="strategyToRun"></param>
        ''' <returns></returns>
        Public Overrides Async Function SubscribeStrategyAsync(ByVal strategyToRun As Strategy) As Task
            logger.Debug("ExecuteStrategyAsync, strategyToRun:{0}", strategyToRun.ToString)
            _cts.Token.ThrowIfCancellationRequested()

            If strategyToRun IsNot Nothing Then
                If _AllStrategies Is Nothing Then _AllStrategies = New List(Of Strategy)

                'Remove this strategy if already exists

                _AllStrategies.RemoveAll(Function(x)
                                             Return x.GetType Is strategyToRun.GetType
                                         End Function)

                _AllStrategies.Add(strategyToRun)
                'Remove and add fresh handlers to be cautious
                RemoveHandler strategyToRun.HeartbeatEx, AddressOf OnHeartbeatEx
                RemoveHandler strategyToRun.WaitingForEx, AddressOf OnWaitingForEx
                RemoveHandler strategyToRun.DocumentRetryStatusEx, AddressOf OnDocumentRetryStatusEx
                RemoveHandler strategyToRun.DocumentDownloadCompleteEx, AddressOf OnDocumentDownloadCompleteEx
                RemoveHandler strategyToRun.NewItemAdded, AddressOf OnNewItemAdded
                RemoveHandler strategyToRun.EndOfTheDay, AddressOf OnEndOfTheDay

                AddHandler strategyToRun.HeartbeatEx, AddressOf OnHeartbeatEx
                AddHandler strategyToRun.WaitingForEx, AddressOf OnWaitingForEx
                AddHandler strategyToRun.DocumentRetryStatusEx, AddressOf OnDocumentRetryStatusEx
                AddHandler strategyToRun.DocumentDownloadCompleteEx, AddressOf OnDocumentDownloadCompleteEx
                AddHandler strategyToRun.NewItemAdded, AddressOf OnNewItemAdded
                AddHandler strategyToRun.EndOfTheDay, AddressOf OnEndOfTheDay
                OnHeartbeatEx(String.Format("As per the strategy logic, tradable instruments being fetched, strategy:{0}", strategyToRun.ToString), New List(Of Object) From {strategyToRun})
                _cts.Token.ThrowIfCancellationRequested()
                Dim ret As Boolean = Await strategyToRun.CreateTradableStrategyInstrumentsAsync(_AllInstruments, _AllBannedStock).ConfigureAwait(False)

                'Now store the unique instruments across strategies into the local collection
                If strategyToRun.TradableStrategyInstruments IsNot Nothing AndAlso strategyToRun.TradableStrategyInstruments.Count > 0 Then
                    Dim tempList As List(Of IInstrument) = Nothing
                    For Each runningTradableStrategyInstrument In strategyToRun.TradableStrategyInstruments
                        _cts.Token.ThrowIfCancellationRequested()
                        If _AllStrategyUniqueInstruments IsNot Nothing AndAlso _AllStrategyUniqueInstruments.Where(Function(x)
                                                                                                                       Return x.InstrumentIdentifier = runningTradableStrategyInstrument.TradableInstrument.InstrumentIdentifier
                                                                                                                   End Function).Count > 0 Then
                            Continue For
                        End If
                        If tempList Is Nothing Then tempList = New List(Of IInstrument)
                        tempList.Add(runningTradableStrategyInstrument.TradableInstrument)
                    Next
                    If _AllStrategyUniqueInstruments IsNot Nothing Then
                        If tempList IsNot Nothing AndAlso tempList.Count > 0 Then
                            _AllStrategyUniqueInstruments = _AllStrategyUniqueInstruments.Union(tempList)
                        End If
                    Else
                        _AllStrategyUniqueInstruments = tempList
                    End If
                End If
                _cts.Token.ThrowIfCancellationRequested()
                If _AllStrategyUniqueInstruments IsNot Nothing AndAlso _AllStrategyUniqueInstruments.Count > 999 Then
                    Throw New ApplicationException("Can not subscribe more than 999 instruments")
                End If

                _cts.Token.ThrowIfCancellationRequested()
                'Now create instrument mapping
                Await CreateInstrumentMappingTable().ConfigureAwait(False)

                _cts.Token.ThrowIfCancellationRequested()
                'Now we know what are the instruments as per the strategy and their corresponding workers
                If Not ret Then Throw New ApplicationException(String.Format("No instruments fetched that can be traded, strategy:{0}", strategyToRun.ToString))

                'Now create a local collection inside the controller for the strategy instruments that need to be subscribed as a dictionary for easy picking during tick receive
                If _subscribedStrategyInstruments Is Nothing Then _subscribedStrategyInstruments = New Dictionary(Of String, Concurrent.ConcurrentBag(Of StrategyInstrument))
                Dim subscribedInstrumentTokens As List(Of String) = Nothing
                For Each runningTradableStrategyInstrument In strategyToRun.TradableStrategyInstruments
                    _cts.Token.ThrowIfCancellationRequested()
                    Dim instrumentKey As String = runningTradableStrategyInstrument.TradableInstrument.InstrumentIdentifier
                    Dim strategiesToBeSubscribedForThisInstrument As Concurrent.ConcurrentBag(Of StrategyInstrument) = Nothing
                    If _subscribedStrategyInstruments.ContainsKey(instrumentKey) Then
                        strategiesToBeSubscribedForThisInstrument = _subscribedStrategyInstruments(instrumentKey)
                    Else
                        strategiesToBeSubscribedForThisInstrument = New Concurrent.ConcurrentBag(Of StrategyInstrument)
                        _subscribedStrategyInstruments.Add(instrumentKey, strategiesToBeSubscribedForThisInstrument)
                    End If
                    'Remove the current strategy if present already
                    Dim tmpList As List(Of StrategyInstrument) = Nothing
                    While Not strategiesToBeSubscribedForThisInstrument.IsEmpty
                        Dim removeCandidate As StrategyInstrument = Nothing
                        strategiesToBeSubscribedForThisInstrument.TryTake(removeCandidate)
                        If removeCandidate IsNot Nothing Then
                            If removeCandidate.GetType IsNot runningTradableStrategyInstrument.GetType Then
                                If tmpList Is Nothing Then tmpList = New List(Of StrategyInstrument)
                                tmpList.Add(removeCandidate)
                            End If
                        End If
                    End While
                    If tmpList IsNot Nothing AndAlso tmpList.Count > 0 Then
                        For Each tmpListItem In tmpList
                            strategiesToBeSubscribedForThisInstrument.Add(tmpListItem)
                        Next
                    End If
                    'strategiesToBeSubscribedForThisInstrument.TakeWhile(Function(X)
                    '                                                        Return X.GetType Is runningTradableStrategyInstrument.GetType
                    '                                                    End Function)
                    strategiesToBeSubscribedForThisInstrument.Add(runningTradableStrategyInstrument)
                Next

                'Create the candlecreator object - one each for each unique instrument
                FillCandlestickCreator()

                'Create dummy tick so as to trigger an UI response
                Dim execCommand As ExecutionCommands = ExecutionCommands.GetQuotes
                Dim allQuotes As IEnumerable(Of IQuote) = Await ExecuteCommandAsync(execCommand, strategyToRun.TradableInstrumentsAsPerStrategy).ConfigureAwait(False)
                If allQuotes IsNot Nothing AndAlso allQuotes.Count > 0 Then
                    For Each runningQuote As ZerodhaQuote In allQuotes

                        OnTickerTickAsync(New Tick() With {.AveragePrice = runningQuote.AveragePrice,
                                          .Bids = If(runningQuote.WrappedQuote.Bids IsNot Nothing, runningQuote.WrappedQuote.Bids.ToArray, Nothing),
                                          .BuyQuantity = runningQuote.WrappedQuote.BuyQuantity,
                                          .Change = runningQuote.WrappedQuote.Change,
                                          .Close = runningQuote.Close,
                                          .High = runningQuote.High,
                                          .InstrumentToken = runningQuote.InstrumentToken,
                                          .LastPrice = runningQuote.LastPrice,
                                          .LastQuantity = runningQuote.WrappedQuote.LastQuantity,
                                          .LastTradeTime = runningQuote.WrappedQuote.LastTradeTime,
                                          .Low = runningQuote.Low,
                                          .Offers = If(runningQuote.WrappedQuote.Offers IsNot Nothing, runningQuote.WrappedQuote.Offers.ToArray, Nothing),
                                          .OI = runningQuote.WrappedQuote.OI,
                                          .OIDayHigh = runningQuote.WrappedQuote.OIDayHigh,
                                          .OIDayLow = runningQuote.WrappedQuote.OIDayLow,
                                          .Open = runningQuote.Open,
                                          .SellQuantity = runningQuote.WrappedQuote.SellQuantity,
                                          .Timestamp = runningQuote.Timestamp,
                                          .Tradable = True,
                                          .Volume = runningQuote.Volume})
                    Next
                End If

                'Now subscribe to the actual ticker
                _cts.Token.ThrowIfCancellationRequested()
                Await strategyToRun.SubscribeAsync(_APITicker, _APIHistoricalDataFetcher).ConfigureAwait(False)
                _cts.Token.ThrowIfCancellationRequested()

                'OnHeartbeat(String.Format("Executing the strategy by creating relevant instrument workers, strategy:{0}", strategyToRun.ToString))
                'Await strategyToRun.ExecuteAsync().ConfigureAwait(False)
                _cts.Token.ThrowIfCancellationRequested()
            End If
        End Function
        'Public Overrides Async Function MonitorAsync(ByVal strategyToRun As Strategy) As Task
        '    If _AllStrategies IsNot Nothing AndAlso _AllStrategies.Count > 0 Then
        '        Dim tasks As List(Of Task) = Nothing
        '        For Each runningStrategy In _AllStrategies
        '            If tasks Is Nothing Then tasks = New List(Of Task)
        '            tasks.Add(Task.Run(Async Function()
        '                                   Await runningStrategy.MonitorAsync().ConfigureAwait(False)
        '                               End Function))
        '        Next
        '        Try
        '            Await Task.WhenAll(tasks)
        '        Catch ex As Exception
        '            Console.WriteLine(ex)
        '        End Try
        '    End If

        'End Function
        Public Overrides Function CreateDummySingleInstrument(supportedTradingSymbol As String, ByVal instrumentToken As UInteger, ByVal sampleInstrument As IInstrument) As IInstrument
            If supportedTradingSymbol IsNot Nothing AndAlso _APIAdapter IsNot Nothing Then
                Return _APIAdapter.CreateSingleInstrument(supportedTradingSymbol, instrumentToken, sampleInstrument)
            Else
                Return Nothing
            End If
        End Function
        Public Overrides Async Function GetOrderDetailsAsync() As Task(Of Concurrent.ConcurrentBag(Of IBusinessOrder))
            Dim ret As Concurrent.ConcurrentBag(Of IBusinessOrder) = Nothing
            _cts.Token.ThrowIfCancellationRequested()
            Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
            Dim execCommand As ExecutionCommands = ExecutionCommands.GetOrders
            _cts.Token.ThrowIfCancellationRequested()
            Dim allOrders As IEnumerable(Of IOrder) = Await ExecuteCommandAsync(execCommand, Nothing).ConfigureAwait(False)
            _cts.Token.ThrowIfCancellationRequested()
            If allOrders IsNot Nothing AndAlso allOrders.Count > 0 Then
                Dim parentOrders As IEnumerable(Of IOrder) = allOrders.Where(Function(x)
                                                                                 Dim y As ZerodhaOrder = CType(x, ZerodhaOrder)
                                                                                 Return y.WrappedOrder.ParentOrderId Is Nothing
                                                                             End Function)
                For Each parentOrder In parentOrders
                    _cts.Token.ThrowIfCancellationRequested()
                    Dim wrappedParentOrder As ZerodhaOrder = CType(parentOrder, ZerodhaOrder)
                    wrappedParentOrder.LogicalOrderType = IOrder.LogicalTypeOfOrder.Parent
                    Dim targetOrder As IEnumerable(Of IOrder) = Nothing
                    Dim slOrder As IEnumerable(Of IOrder) = Nothing
                    Dim allOrder As IEnumerable(Of IOrder) = Nothing
                    If wrappedParentOrder.WrappedOrder.TransactionType = "BUY" Then
                        _cts.Token.ThrowIfCancellationRequested()
                        slOrder = allOrders.ToList.FindAll(Function(x)
                                                               Dim y As ZerodhaOrder = CType(x, ZerodhaOrder)
                                                               If y.Status = IOrder.TypeOfStatus.Cancelled OrElse
                                                                   y.Status = IOrder.TypeOfStatus.Complete OrElse
                                                                   y.Status = IOrder.TypeOfStatus.Rejected Then
                                                                   Return Nothing
                                                               Else
                                                                   Return y.WrappedOrder.ParentOrderId = parentOrder.OrderIdentifier AndAlso
                                                                 y.WrappedOrder.TriggerPrice <= wrappedParentOrder.WrappedOrder.AveragePrice AndAlso
                                                                 y.WrappedOrder.TriggerPrice <> 0
                                                               End If
                                                           End Function)
                        _cts.Token.ThrowIfCancellationRequested()
                        targetOrder = allOrders.ToList.FindAll(Function(x)
                                                                   Dim y As ZerodhaOrder = CType(x, ZerodhaOrder)
                                                                   If y.Status = IOrder.TypeOfStatus.Cancelled OrElse
                                                                       y.Status = IOrder.TypeOfStatus.Complete OrElse
                                                                       y.Status = IOrder.TypeOfStatus.Rejected Then
                                                                       Return Nothing
                                                                   Else
                                                                       Return y.WrappedOrder.ParentOrderId = parentOrder.OrderIdentifier AndAlso
                                                                    y.WrappedOrder.Price > wrappedParentOrder.WrappedOrder.AveragePrice AndAlso
                                                                    y.WrappedOrder.Price <> 0
                                                                   End If
                                                               End Function)
                        _cts.Token.ThrowIfCancellationRequested()
                        allOrder = allOrders.ToList.FindAll(Function(x)
                                                                Dim y As ZerodhaOrder = CType(x, ZerodhaOrder)
                                                                If y.Status = IOrder.TypeOfStatus.Cancelled OrElse
                                                                    y.Status = IOrder.TypeOfStatus.Complete OrElse
                                                                    y.Status = IOrder.TypeOfStatus.Rejected Then
                                                                    Return y.WrappedOrder.ParentOrderId = parentOrder.OrderIdentifier
                                                                Else
                                                                    Return Nothing
                                                                End If
                                                            End Function)
                    ElseIf wrappedParentOrder.WrappedOrder.TransactionType = "SELL" Then
                        _cts.Token.ThrowIfCancellationRequested()
                        slOrder = allOrders.ToList.FindAll(Function(x)
                                                               Dim y As ZerodhaOrder = CType(x, ZerodhaOrder)
                                                               If y.Status = IOrder.TypeOfStatus.Cancelled OrElse
                                                                   y.Status = IOrder.TypeOfStatus.Complete OrElse
                                                                   y.Status = IOrder.TypeOfStatus.Rejected Then
                                                                   Return Nothing
                                                               Else
                                                                   Return y.WrappedOrder.ParentOrderId = parentOrder.OrderIdentifier AndAlso
                                                                y.WrappedOrder.TriggerPrice >= wrappedParentOrder.WrappedOrder.AveragePrice AndAlso
                                                                y.WrappedOrder.TriggerPrice <> 0
                                                               End If
                                                           End Function)
                        _cts.Token.ThrowIfCancellationRequested()
                        targetOrder = allOrders.ToList.FindAll(Function(x)
                                                                   Dim y As ZerodhaOrder = CType(x, ZerodhaOrder)
                                                                   If y.Status = IOrder.TypeOfStatus.Cancelled OrElse
                                                                       y.Status = IOrder.TypeOfStatus.Complete OrElse
                                                                       y.Status = IOrder.TypeOfStatus.Rejected Then
                                                                       Return Nothing
                                                                   Else
                                                                       Return y.WrappedOrder.ParentOrderId = parentOrder.OrderIdentifier AndAlso
                                                                    y.WrappedOrder.Price < wrappedParentOrder.WrappedOrder.AveragePrice AndAlso
                                                                    y.WrappedOrder.Price <> 0
                                                                   End If
                                                               End Function)
                        _cts.Token.ThrowIfCancellationRequested()
                        allOrder = allOrders.ToList.FindAll(Function(x)
                                                                Dim y As ZerodhaOrder = CType(x, ZerodhaOrder)
                                                                If y.Status = IOrder.TypeOfStatus.Cancelled OrElse
                                                                    y.Status = IOrder.TypeOfStatus.Complete OrElse
                                                                    y.Status = IOrder.TypeOfStatus.Rejected Then
                                                                    Return y.WrappedOrder.ParentOrderId = parentOrder.OrderIdentifier
                                                                Else
                                                                    Return Nothing
                                                                End If
                                                            End Function)
                    End If
                    If slOrder IsNot Nothing AndAlso slOrder.Count > 0 Then
                        For Each runningOrder In slOrder
                            runningOrder.LogicalOrderType = IOrder.LogicalTypeOfOrder.Stoploss
                        Next
                    End If
                    If targetOrder IsNot Nothing AndAlso targetOrder.Count > 0 Then
                        For Each runningOrder In targetOrder
                            runningOrder.LogicalOrderType = IOrder.LogicalTypeOfOrder.Target
                        Next
                    End If
                    If allOrder IsNot Nothing AndAlso allOrder.Count > 0 Then
                        For Each runningOrder In allOrder
                            If runningOrder.OrderType = IOrder.TypeOfOrder.Limit AndAlso runningOrder.TriggerPrice = 0 Then
                                runningOrder.LogicalOrderType = IOrder.LogicalTypeOfOrder.Target
                            ElseIf runningOrder.OrderType = IOrder.TypeOfOrder.Limit AndAlso runningOrder.TriggerPrice <> 0 Then
                                runningOrder.LogicalOrderType = IOrder.LogicalTypeOfOrder.Stoploss
                            ElseIf runningOrder.OrderType = IOrder.TypeOfOrder.SL Then
                                runningOrder.LogicalOrderType = IOrder.LogicalTypeOfOrder.Stoploss
                            End If
                        Next
                    End If
                    Dim businessOrder As New BusinessOrder With {.ParentOrderIdentifier = parentOrder.OrderIdentifier,
                                                                        .ParentOrder = parentOrder,
                                                                        .SLOrder = slOrder,
                                                                        .AllOrder = allOrder,
                                                                        .TargetOrder = targetOrder}

                    If ret Is Nothing Then ret = New Concurrent.ConcurrentBag(Of IBusinessOrder)
                    ret.Add(businessOrder)


                    ''This for loop needs to be after the order is published
                    'If _subscribedStrategyInstruments IsNot Nothing AndAlso _subscribedStrategyInstruments.Count > 0 AndAlso
                    '_subscribedStrategyInstruments.ContainsKey(parentOrder.InstrumentIdentifier) Then
                    '    For Each runningStrategyInstrument In _subscribedStrategyInstruments(parentOrder.InstrumentIdentifier)
                    '        If parentOrder.Tag IsNot Nothing AndAlso parentOrder.Tag.Contains(runningStrategyInstrument.GenerateTag()) Then
                    '            Dim unionOfAllOrders As IEnumerable(Of IOrder) = businessOrder.SLOrder.Union(businessOrder.AllOrder.Union(businessOrder.TargetOrder))
                    '            unionOfAllOrders = Utilities.Collections.ConcatSingle(Of IOrder)(unionOfAllOrders, businessOrder.ParentOrder)
                    '            If unionOfAllOrders IsNot Nothing AndAlso unionOfAllOrders.Count > 0 Then
                    '                For Each runningOrder In unionOfAllOrders
                    '                    runningStrategyInstrument.ProcessOrderAsync(runningOrder)
                    '                Next
                    '            End If
                    '        End If
                    '    Next
                    'End If
                Next
            End If
            Return ret
        End Function
        Public Overrides Async Function GetHoldingDetailsAsync() As Task(Of ConcurrentBag(Of IHolding))
            Dim ret As Concurrent.ConcurrentBag(Of IHolding) = Nothing
            _cts.Token.ThrowIfCancellationRequested()
            Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
            Dim execCommand As ExecutionCommands = ExecutionCommands.GetHoldings
            _cts.Token.ThrowIfCancellationRequested()
            Dim allHoldings As IEnumerable(Of IHolding) = Await ExecuteCommandAsync(execCommand, Nothing).ConfigureAwait(False)
            _cts.Token.ThrowIfCancellationRequested()
            If allHoldings IsNot Nothing AndAlso allHoldings.Count > 0 Then
                For Each runningHolding In allHoldings
                    If ret Is Nothing Then ret = New ConcurrentBag(Of IHolding)
                    ret.Add(runningHolding)
                Next
            End If
            Return ret
        End Function
        Public Overrides Async Function GetPositionDetailsAsync() As Task(Of ConcurrentBag(Of IPosition))
            Dim ret As Concurrent.ConcurrentBag(Of IPosition) = Nothing
            _cts.Token.ThrowIfCancellationRequested()
            Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
            Dim execCommand As ExecutionCommands = ExecutionCommands.GetPositions
            _cts.Token.ThrowIfCancellationRequested()
            Dim allPositions As IPositionResponse = Await ExecuteCommandAsync(execCommand, Nothing).ConfigureAwait(False)
            _cts.Token.ThrowIfCancellationRequested()
            If allPositions IsNot Nothing AndAlso allPositions.Net IsNot Nothing AndAlso allPositions.Net.Count > 0 Then
                For Each runningPosition In allPositions.Net
                    If ret Is Nothing Then ret = New ConcurrentBag(Of IPosition)
                    ret.Add(runningPosition)
                Next
            End If
            Return ret
        End Function
#End Region

#Region "Fetcher Events"
        Public Overrides Async Function CloseFetcherIfConnectedAsync(ByVal forceClose As Boolean) As Task
            If _APIHistoricalDataFetcher IsNot Nothing Then Await _APIHistoricalDataFetcher.CloseFetcherIfConnectedAsync(forceClose).ConfigureAwait(False)
        End Function

        Public Async Sub OnFetcherCandlesAsync(ByVal instrumentIdentifier As String, ByVal historicalCandlesJSONDict As Dictionary(Of String, Object))
            'logger.Debug("OnFetcherCandlesAsync, parameteres:{0},{1}",instrumentIdentifier, Utils.JsonSerialize(historicalCandlesJSONDict))
            Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
            If _rawPayloadCreators IsNot Nothing AndAlso _rawPayloadCreators.ContainsKey(instrumentIdentifier) Then
                _rawPayloadCreators(instrumentIdentifier).GetChartFromHistoricalAsync(historicalCandlesJSONDict)
            End If
        End Sub

        Public Overrides Sub OnFetcherError(ByVal instrumentIdentifier As String, ByVal errorMessage As String)
            logger.Debug("OnFetcherError, errorMessage:{0} ,instrumentIdentifier:{1}", errorMessage, instrumentIdentifier)
            MyBase.OnFetcherError(instrumentIdentifier, errorMessage)
            'If errorMessage.Contains("403") Then OnSessionExpireAsync()
        End Sub
#End Region

#Region "Ticker Events"
        Public Overrides Async Function CloseTickerIfConnectedAsync() As Task
            If _APITicker IsNot Nothing Then Await _APITicker.CloseTickerIfConnectedAsync().ConfigureAwait(False)
        End Function

        Public Overrides Sub OnTickerConnect()
            logger.Debug("OnTickerConnect, parameters:Nothing", Me.ToString)
            MyBase.OnTickerConnect()
        End Sub
        Public Overrides Sub OnTickerClose()
            logger.Debug("OnTickerClose, parameters:Nothing", Me.ToString)
            MyBase.OnTickerClose()
        End Sub
        Public Overrides Sub OnTickerError(ByVal errorMessage As String)
            logger.Debug("OnTickerError, errorMessage:{0}", errorMessage)
            If _APITicker IsNot Nothing Then
                OnTickerErrorWithStatus(_APITicker.IsConnected, errorMessage)
            Else
                OnTickerErrorWithStatus(False, errorMessage)
            End If
            MyBase.OnTickerError(errorMessage)
            If errorMessage.Contains("403") Then OnSessionExpireAsync()
        End Sub
        Public Overrides Sub OnTickerErrorWithStatus(ByVal isConnected As Boolean, ByVal errorMessage As String)
            logger.Debug("OnTickerErrorWithStatus, isConnected:{0}, errorMessage:{1}", isConnected, errorMessage)
            MyBase.OnTickerErrorWithStatus(isConnected, errorMessage)
        End Sub
        Public Overrides Sub OnTickerNoReconnect()
            logger.Debug("OnTickerNoReconnect, parameters:Nothing", Me.ToString)
            'OnHeartbeat("Ticker, not Reconnecting")
            MyBase.OnTickerNoReconnect()
        End Sub
        Public Overrides Sub OnTickerReconnect()
            logger.Debug("OnTickerReconnect, parameters:Nothing", Me.ToString)
            MyBase.OnTickerReconnect()
        End Sub
        Public Async Sub OnTickerTickAsync(ByVal tickData As Tick)
            Await Task.Delay(1, _cts.Token).ConfigureAwait(False)

            Dim runningTick As New ZerodhaTick() With {.WrappedTick = tickData}
            Dim runningInstruments As IEnumerable(Of IInstrument) = _AllStrategyUniqueInstruments.Where(Function(x)
                                                                                                            Return x.InstrumentIdentifier = tickData.InstrumentToken
                                                                                                        End Function)

            Dim change As Boolean = False
            If runningInstruments IsNot Nothing AndAlso runningInstruments.Count > 0 Then
                change = runningInstruments.FirstOrDefault.LastTick IsNot Nothing AndAlso
                        (runningTick.OI <> runningInstruments.FirstOrDefault.LastTick.OI OrElse
                        runningTick.Volume <> runningInstruments.FirstOrDefault.LastTick.Volume OrElse
                        runningTick.LastPrice <> runningInstruments.FirstOrDefault.LastTick.LastPrice)

                runningInstruments.FirstOrDefault.LastTick = runningTick
            End If

            'If change Then
            '    logger.Fatal("TickData, Token,{0},Date,{1},Time,{2},LastPrice,{3},Volume,{4},OI,{5}",
            '             tickData.InstrumentToken, tickData.Timestamp.Value.ToShortDateString, tickData.Timestamp.Value.ToLongTimeString,
            '             tickData.LastPrice, tickData.Volume, tickData.OI)
            'End If

            If _rawPayloadCreators IsNot Nothing AndAlso _rawPayloadCreators.ContainsKey(tickData.InstrumentToken) Then
                _rawPayloadCreators(tickData.InstrumentToken).GetChartFromTickAsync(runningTick)
            End If
            If _subscribedStrategyInstruments IsNot Nothing AndAlso _subscribedStrategyInstruments.Count > 0 AndAlso
                _subscribedStrategyInstruments.ContainsKey(tickData.InstrumentToken) Then
                'This loop is for population of ticks payload. As ticks payload depends on instrument so loop should exit after 1 iteration.
                For Each runningStrategyInstrument In _subscribedStrategyInstruments(tickData.InstrumentToken)
                    If runningStrategyInstrument.ParentStrategy.IsTickPopulationNeeded Then
                        runningStrategyInstrument.TradableInstrument.TickPayloads.Add(runningTick)
                    End If
                    Exit For
                Next

                'This for loop needs to be after the tick is published
                For Each runningStrategyInstrument In _subscribedStrategyInstruments(tickData.InstrumentToken)
                    runningStrategyInstrument.HandleTickTriggerToUIETCAsync()
                Next
            End If
        End Sub
        Public Async Sub OnTickerOrderUpdateAsync(ByVal orderData As Order)
            'logger.Debug("OnTickerOrderUpdateAsync, orderData:{0}", Utils.JsonSerialize(orderData))
            Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
            If orderData.Status = "COMPLETE" OrElse
                orderData.Status = "MODIFIED" OrElse
                orderData.Status = "CANCELLED" OrElse
                orderData.Status = "OPEN" OrElse
                orderData.Status = "TRIGGER PENDING" Then
                FillOrderDetailsAsync()
                FillPositionDetailsAsync()
            End If
        End Sub
#End Region

#Region "Collector Events"
        Public Overrides Async Function CloseCollectorIfConnectedAsync(ByVal forceClose As Boolean) As Task
            If _APIInformationCollector IsNot Nothing Then Await _APIInformationCollector.CloseCollectorIfConnectedAsync(forceClose).ConfigureAwait(False)
        End Function
        Dim ctr As Integer = 0
        Public Async Sub OnCollectorInformationAsync(ByVal information As Object, ByVal typeOfInformation As APIInformationCollector.InformationType)
            'logger.Debug("OnCollectorInformationAsync, parameteres:{0},{1}",instrumentIdentifier, Utils.JsonSerialize(historicalCandlesJSONDict))
            Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
            If information IsNot Nothing Then
                Select Case typeOfInformation
                    Case APIInformationCollector.InformationType.GetOrderDetails
                        Dim orderDetails As Concurrent.ConcurrentBag(Of IBusinessOrder) = CType(information, Concurrent.ConcurrentBag(Of IBusinessOrder))
                        If orderDetails IsNot Nothing AndAlso orderDetails.Count > 0 Then
                            For Each orderData In orderDetails
                                _cts.Token.ThrowIfCancellationRequested()
                                If _AllStrategies IsNot Nothing AndAlso _AllStrategies.Count > 0 Then
                                    For Each strategyToRun In _AllStrategies
                                        _cts.Token.ThrowIfCancellationRequested()
                                        Await strategyToRun.ProcessOrderAsync(orderData).ConfigureAwait(False)
                                        strategyToRun.IsFirstTimeInformationCollected = True
                                    Next
                                End If
                            Next
                        End If
                    Case APIInformationCollector.InformationType.GetPositionDetails
                        Dim postionDetails As Concurrent.ConcurrentBag(Of IPosition) = CType(information, Concurrent.ConcurrentBag(Of IPosition))
                        If postionDetails IsNot Nothing AndAlso postionDetails.Count > 0 Then
                            For Each positionData In postionDetails
                                _cts.Token.ThrowIfCancellationRequested()
                                If _AllStrategies IsNot Nothing AndAlso _AllStrategies.Count > 0 Then
                                    For Each strategyToRun In _AllStrategies
                                        _cts.Token.ThrowIfCancellationRequested()
                                        Await strategyToRun.ProcessPositionAsync(positionData).ConfigureAwait(False)
                                        strategyToRun.IsFirstTimeInformationCollected = True
                                    Next
                                End If
                            Next
                        End If
                    Case Else
                        Throw New NotImplementedException
                End Select
            Else
                If _AllStrategies IsNot Nothing AndAlso _AllStrategies.Count > 0 Then
                    For Each strategyToRun In _AllStrategies
                        _cts.Token.ThrowIfCancellationRequested()
                        strategyToRun.IsFirstTimeInformationCollected = True
                    Next
                End If
            End If
        End Sub

        Public Overrides Sub OnCollectorError(ByVal errorMessage As String)
            logger.Debug("OnCollectorError, errorMessage:{0}", errorMessage)
            MyBase.OnCollectorError(errorMessage)
            'If errorMessage.Contains("403") Then OnSessionExpireAsync()
        End Sub
#End Region

    End Class
End Namespace