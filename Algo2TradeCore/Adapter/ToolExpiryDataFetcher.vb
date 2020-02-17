Imports System.Threading
Imports NLog
Imports Utilities.Network
Imports Utilities.DAL
Imports System.IO
Imports System.Net.Http
Imports System.Net

Namespace Adapter
    Public Class ToolExpiryDataFetcher
        Implements IDisposable

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

        Private ReadOnly _cts As CancellationTokenSource

        Public Sub New(ByVal canceller As CancellationTokenSource)
            _cts = canceller
        End Sub

        Private Function GetToolExpiryURL() As String
            Dim ret As String = Nothing
            Dim toolExpiryURL As String = "http://algo2trade.com/a.txt"
            ret = toolExpiryURL
            Return ret
        End Function

        Public Async Function GetToolExpiryDataAsync() As Task(Of Dictionary(Of String, Date))
            Dim ret As Dictionary(Of String, Date) = Nothing

            ServicePointManager.Expect100Continue = False
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
            ServicePointManager.ServerCertificateValidationCallback = Function(s, Ca, CaC, sslPE)
                                                                          Return True
                                                                      End Function

            Dim proxyToBeUsed As HttpProxy = Nothing
            Using browser As New HttpBrowser(proxyToBeUsed, Net.DecompressionMethods.GZip, New TimeSpan(0, 1, 0), _cts)
                AddHandler browser.DocumentDownloadComplete, AddressOf OnDocumentDownloadComplete
                'AddHandler browser.Heartbeat, AddressOf OnHeartbeat
                AddHandler browser.WaitingFor, AddressOf OnWaitingFor
                AddHandler browser.DocumentRetryStatus, AddressOf OnDocumentRetryStatus
                'Get to the landing page first
                Try
                    Dim l As Tuple(Of Uri, Object) = Await browser.NonPOSTRequestAsync(GetToolExpiryURL(),
                                                                                    HttpMethod.Get,
                                                                                    Nothing,
                                                                                    True,
                                                                                    Nothing,
                                                                                    True,
                                                                                    "text/plain").ConfigureAwait(False)
                    If l IsNot Nothing AndAlso l.Item2 IsNot Nothing Then
                        Dim expiryData As String() = l.Item2.ToString.Split(vbCrLf)
                        For Each runningData In expiryData
                            Dim data As String() = runningData.Trim.Split(",")
                            If ret Is Nothing Then ret = New Dictionary(Of String, Date)
                            ret.Add(data(0), Date.Parse(data(1)))
                        Next
                    End If
                Catch ex As Exception
                    logger.Error(ex.ToString)
                End Try
                RemoveHandler browser.DocumentDownloadComplete, AddressOf OnDocumentDownloadComplete
                RemoveHandler browser.Heartbeat, AddressOf OnHeartbeat
                RemoveHandler browser.WaitingFor, AddressOf OnWaitingFor
                RemoveHandler browser.DocumentRetryStatus, AddressOf OnDocumentRetryStatus
            End Using
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
End Namespace