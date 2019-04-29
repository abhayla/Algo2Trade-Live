Imports System.Runtime.Serialization
Namespace Exceptions
    ''' <summary>
    ''' Custom class for handling business exceptions
    ''' </summary>
    <Serializable()>
    Public MustInherit Class AdapterBusinessException
        Inherits ApplicationException
        Implements IDisposable

#Region "Enums"
        Public Enum TypeOfException
            RMSError = 1
            GeneralException
            TokenException
            PermissionException
            OrderException
            InputException
            DataException
            NetworkException
            None
        End Enum
#End Region

#Region "Public Properties"
        Public Property ExceptionType As TypeOfException
        Public Property ExceptionDetails As String
#End Region

#Region "Constructors"
        ''' <summary>
        ''' Default constructor
        ''' </summary>

        Public Sub New()
        End Sub

        ''' <summary>
        ''' Creates object with only the exception message
        ''' </summary>
        ''' <param name="message">Message associated with the exception</param>
        Public Sub New(ByVal message As String)
            MyBase.New(message)
        End Sub

        Public Sub New(ByVal message As String, ByVal exceptionType As TypeOfException)
            MyBase.New(message)
            Me.ExceptionType = exceptionType
        End Sub

        ''' <summary>
        ''' Creates object with only the exception message
        ''' </summary>
        ''' <param name="message">Message associated with the exception</param>
        Public Sub New(ByVal message As String, ByVal exceptionDetails As String)
            MyBase.New(message)
            Me.ExceptionDetails = exceptionDetails
        End Sub

        Public Sub New(ByVal message As String, ByVal exceptionDetails As String, ByVal exceptionType As TypeOfException)
            MyBase.New(message)
            Me.ExceptionType = exceptionType
            Me.ExceptionDetails = exceptionDetails
        End Sub

        ''' <summary>
        ''' Creates object with the exception message and inner exception
        ''' </summary>
        ''' <param name="message">Message associated with the exception</param>
        ''' <param name="inner">Inner exception associated with the exception</param>
        Public Sub New(ByVal message As String, ByVal inner As Exception)
            MyBase.New(message, inner)
        End Sub

        Public Sub New(ByVal message As String, ByVal inner As Exception, ByVal exceptionType As TypeOfException)
            MyBase.New(message, inner)
            Me.ExceptionType = exceptionType
        End Sub

        ''' <summary>
        ''' Creates object with the exception message and inner exception
        ''' </summary>
        ''' <param name="message">Message associated with the exception</param>
        ''' <param name="inner">Inner exception associated with the exception</param>
        Public Sub New(ByVal message As String, ByVal inner As Exception, ByVal exceptionDetails As String)
            MyBase.New(message, inner)
            Me.ExceptionDetails = exceptionDetails
        End Sub

        Public Sub New(ByVal message As String, ByVal inner As Exception, ByVal exceptionDetails As String, ByVal exceptionType As TypeOfException)
            MyBase.New(message, inner)
            Me.ExceptionDetails = exceptionDetails
            Me.ExceptionType = exceptionType
        End Sub

        ''' <summary>
        ''' Creates object which is seralizable
        ''' </summary>
        ''' <param name="info">Serialization information</param>
        ''' <param name="context">Stream to be serialized</param>
        Protected Sub New(ByVal info As SerializationInfo, ByVal context As StreamingContext)
            MyBase.New(info, context)
        End Sub
#End Region

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
