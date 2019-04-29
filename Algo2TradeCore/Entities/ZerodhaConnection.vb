Namespace Entities
    Public Class ZerodhaConnection
        Implements IConnection
        Public Property RequestToken As String Implements IConnection.RequestToken
        Public ReadOnly Property AccessToken As String Implements IConnection.AccessToken
            Get
                If ZerodhaUser IsNot Nothing AndAlso CType(ZerodhaUser, ZerodhaUser).WrappedUser.AccessToken IsNot Nothing Then
                    Return CType(ZerodhaUser, ZerodhaUser).WrappedUser.AccessToken
                Else
                    Return Nothing
                End If
            End Get
        End Property
        Public ReadOnly Property PublicToken As String Implements IConnection.PublicToken
            Get
                If ZerodhaUser IsNot Nothing AndAlso CType(ZerodhaUser, ZerodhaUser).WrappedUser.PublicToken IsNot Nothing Then
                    Return CType(ZerodhaUser, ZerodhaUser).WrappedUser.PublicToken
                Else
                    Return Nothing
                End If
            End Get
        End Property
        Public Property ZerodhaUser As IUser Implements IConnection.APIUser

        Public ReadOnly Property Broker As APISource Implements IConnection.Broker
            Get
                Return APISource.Zerodha
            End Get
        End Property
    End Class
End Namespace