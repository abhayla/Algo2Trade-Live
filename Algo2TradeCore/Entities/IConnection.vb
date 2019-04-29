Namespace Entities
    Public Interface IConnection
        Property RequestToken As String
        ReadOnly Property AccessToken As String
        ReadOnly Property PublicToken As String

        Property APIUser As IUser
        ReadOnly Property Broker As APISource
    End Interface
End Namespace
