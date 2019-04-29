Imports System.IO

Namespace Entities.UserSettings
    <Serializable>
    Public Class ControllerUserInputs
        Public Shared Property Filename As String = Path.Combine(My.Application.Info.DirectoryPath, "UserInputs.Controller.a2t")
        Public Property UserDetails As IUser
        Public Property GetInformationDelay As Integer
        Public Property BackToBackOrderCoolOffDelay As Integer
        Public Property ExchangeDetails As Dictionary(Of String, Exchange)

        Private _ForceRestartTime As Date
        Public Property ForceRestartTime As Date
            Get
                Return New Date(Now.Year, Now.Month, Now.Day, _ForceRestartTime.Hour, _ForceRestartTime.Minute, _ForceRestartTime.Second)
            End Get
            Set(value As Date)
                _ForceRestartTime = value
            End Set
        End Property

    End Class
End Namespace
