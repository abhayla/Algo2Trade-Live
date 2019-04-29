Namespace Exceptions
    Public Class ZerodhaBusinessException
        Inherits AdapterBusinessException
        Implements IDisposable

        Public Sub New(ByVal message As String, ByVal inner As Exception, ByVal exceptionType As TypeOfException)
            MyBase.New(message, inner, exceptionType)
        End Sub
    End Class
End Namespace
