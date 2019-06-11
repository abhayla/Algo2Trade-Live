Imports System.Threading
Imports Algo2TradeCore.Controller
Imports Algo2TradeCore.Entities
Imports Algo2TradeCore.Exceptions
Imports NLog

Namespace Adapter
    Public Class ZerodhaInformationCollector
        Inherits APIInformationCollector
        Implements IDisposable

#Region "Logging and Status Progress"
        Public Shared Shadows logger As Logger = LogManager.GetCurrentClassLogger
#End Region

#Region "Events/Event handlers specific to the derived class"
        Public Event CollectorInformationAsync(ByVal information As Object, ByVal typeOfInformation As InformationType)
        Public Event CollectorError(ByVal msg As String)
        'The below functions are needed to allow the derived classes to raise the above two events
        Protected Overridable Async Function OnCollectorInformationAsync(ByVal information As Object, ByVal typeOfInformation As InformationType) As Task
            Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
            RaiseEvent CollectorInformationAsync(information, typeOfInformation)
        End Function
        Protected Overridable Sub OnCollectorError(ByVal msg As String)
            RaiseEvent CollectorError(msg)
        End Sub
#End Region

        Public Sub New(ByVal associatedParentController As APIStrategyController,
                       ByVal pollingFrequency As Integer,
                       ByVal canceller As CancellationTokenSource)
            MyBase.New(associatedParentController, pollingFrequency, canceller)
            StartPollingAsync()
        End Sub

        Public Overrides Async Function ConnectCollectorAsync() As Task
            _cts.Token.ThrowIfCancellationRequested()
            Await Task.Delay(0, _cts.Token).ConfigureAwait(False)
            Dim currentZerodhaStrategyController As ZerodhaStrategyController = CType(ParentController, ZerodhaStrategyController)

            RemoveHandler Me.CollectorInformationAsync, AddressOf currentZerodhaStrategyController.OnCollectorInformationAsync
            RemoveHandler Me.CollectorError, AddressOf currentZerodhaStrategyController.OnCollectorError
            _cts.Token.ThrowIfCancellationRequested()
            AddHandler Me.CollectorInformationAsync, AddressOf currentZerodhaStrategyController.OnCollectorInformationAsync
            AddHandler Me.CollectorError, AddressOf currentZerodhaStrategyController.OnCollectorError
        End Function


        Protected Overrides Async Function StartPollingAsync() As Task
            'logger.Debug("{0}->StartPollingAsync, parameters:Nothing", Me.ToString)
            Try
                _stopPollRunning = False
                _isPollRunning = False
                Dim apiConnectionBeingUsed As ZerodhaConnection = Me.ParentController.APIConnection
                While True
                    If _stopPollRunning Then
                        Exit While
                    End If
                    _isPollRunning = True
                    _cts.Token.ThrowIfCancellationRequested()

                    Try
                        If Now >= Me.ParentController.UserInputs.ForceRestartTime AndAlso
                            Now <= Me.ParentController.UserInputs.ForceRestartTime.AddSeconds(_pollingFrequency * 2) Then
                            Await Task.Delay(1000 * ((_pollingFrequency * 2) - 5), _cts.Token).ConfigureAwait(False)
                            Throw New ForceExitException()
                        End If

                        Await GetOrderUpdatesAsync().ConfigureAwait(False)
                    Catch fex As ForceExitException
                        logger.Error(fex)
                        OnCollectorError(fex.Message)
                        Me.ParentController.OrphanException = fex
                    Catch aex As AdapterBusinessException
                        logger.Error(aex)
                        OnCollectorError(aex.Message)
                        Select Case aex.ExceptionType
                            Case AdapterBusinessException.TypeOfException.PermissionException
                                Me.ParentController.OrphanException = aex
                        End Select
                    Catch ex As Exception
                        'Neglect error as in the next minute, it will be run again,
                        'till that time tick based candles will be used
                        logger.Warn(ex)
                        If Not ex.GetType Is GetType(OperationCanceledException) Then
                            OnCollectorError(ex.Message)
                        End If
                    End Try

                    If Me.ParentController.APIConnection Is Nothing OrElse apiConnectionBeingUsed Is Nothing OrElse
                        (Me.ParentController.APIConnection IsNot Nothing AndAlso apiConnectionBeingUsed IsNot Nothing AndAlso
                        Not Me.ParentController.APIConnection.Equals(apiConnectionBeingUsed)) Then
                        Debug.WriteLine("Exiting start polling")
                        Exit While
                    End If
                    _cts.Token.ThrowIfCancellationRequested()
                    Await Task.Delay(_pollingFrequency * 1000, _cts.Token).ConfigureAwait(False)
                End While
            Catch ex As Exception
                logger.Error("Information Collector:{0}, error:{1}", Me.ToString, ex.ToString)
                Me.ParentController.OrphanException = ex
            Finally
                _isPollRunning = False
            End Try
        End Function

        Protected Overrides Async Function GetOrderUpdatesAsync() As Task
            Dim orderDetails As Concurrent.ConcurrentBag(Of IBusinessOrder) = Await Me.ParentController.GetOrderDetailsAsync().ConfigureAwait(False)
            Await OnCollectorInformationAsync(orderDetails, InformationType.GetOrderDetails).ConfigureAwait(False)
        End Function

        Public Overrides Function ToString() As String
            Return Me.GetType.ToString
        End Function

        Public Overrides Function IsConnected() As Boolean
            Return _isPollRunning
        End Function

        Public Overrides Async Function CloseCollectorIfConnectedAsync(ByVal forceClose As Boolean) As Task
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
