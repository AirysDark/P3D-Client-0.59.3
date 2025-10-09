Namespace GameJolt
    Public Class Classified
        ' === Obfuscated (Base64) values ===
        Public Shared ReadOnly GameJolt_Game_ID As String = "MTIz"                 ' "123"
        Public Shared ReadOnly GameJolt_Game_Key As String = "bG9jYWwtdGVzdC1rZXk=" ' "local-test-key"

        ' === Core host/base ===
        Public Shared ReadOnly ApiHost As String = "http://127.0.0.1:8080"
        Public Shared ReadOnly ApiBase As String = ApiHost & "/api/game/v1_1"

        ' === Website/UI routes ===
        Public Shared ReadOnly RegisterUrl As String = ApiHost & "/register"
        Public Shared ReadOnly LoginUrl As String = ApiHost & "/login"
        Public Shared ReadOnly DashboardUrl As String = ApiHost & "/dashboard"
        Public Shared ReadOnly TokenManageUrl As String = ApiHost & "/register" ' fallback

        ' === Static asset endpoints ===
        Public Shared ReadOnly Remote_Texture_URL As String = ApiHost & "/assets/textures/"
        Public Shared ReadOnly Remote_Emblem_URL As String = ApiHost & "/assets/emblems/"
        Public Shared ReadOnly Remote_Avatar_URL As String = ApiHost & "/assets/avatars/"
        Public Shared ReadOnly Remote_User_URL As String = ApiHost & "/assets/users/"

        ' === API endpoints ===
        Public Shared ReadOnly Users_Auth As String = ApiBase & "/users/auth"
        Public Shared ReadOnly Users_Register As String = ApiBase & "/users/register"
        Public Shared ReadOnly Users_Get As String = ApiBase & "/users"

        Public Shared ReadOnly Sessions_Open As String = ApiBase & "/sessions/open"
        Public Shared ReadOnly Sessions_Ping As String = ApiBase & "/sessions/ping"
        Public Shared ReadOnly Sessions_Close As String = ApiBase & "/sessions/close"

        Public Shared ReadOnly DS_Get As String = ApiBase & "/data-store/get"
        Public Shared ReadOnly DS_Set As String = ApiBase & "/data-store/set"
        Public Shared ReadOnly DS_Update As String = ApiBase & "/data-store/update"
        Public Shared ReadOnly DS_GetKeys As String = ApiBase & "/data-store/get-keys"
        Public Shared ReadOnly DS_Remove As String = ApiBase & "/data-store/remove"
        Public Shared ReadOnly Batch As String = ApiBase & "/batch"

        Public Shared ReadOnly Trophies_List As String = ApiBase & "/trophies"
        Public Shared ReadOnly Trophies_Add As String = ApiBase & "/trophies/add-achieved"
        Public Shared ReadOnly Trophies_Remove As String = ApiBase & "/trophies/remove-achieved"

        Public Shared ReadOnly Scores_List As String = ApiBase & "/scores"
        Public Shared ReadOnly Scores_Add As String = ApiBase & "/scores/add"
        Public Shared ReadOnly Scores_GetRank As String = ApiBase & "/scores/get-rank"

        Public Shared ReadOnly Friends_List As String = ApiBase & "/friends"
        Public Shared ReadOnly Friends_Add As String = ApiBase & "/friends/add"
        Public Shared ReadOnly Friends_Remove As String = ApiBase & "/friends/remove"

        ' === Encryption keys for local storage ===
        ' Base64("p3d-local-dev") = "cDNkLWxvY2FsLWRldg=="
        ' Base64("p3d-local-dev-salt") = "cDNkLWxvY2FsLWRldi1zYWx0"
        Public Shared ReadOnly Encryption_Password As String = "cDNkLWxvY2FsLWRldg=="
        Public Shared ReadOnly Encryption_Salt As String = "cDNkLWxvY2FsLWRldi1zYWx0"

        ' === Signature defaults ===
        Public Shared ReadOnly SignatureQueryKey As String = "signature"
        Public Shared ReadOnly DefaultFormat As String = "keypair"

        ' === Helper: decoded (plain) values ===
        Public Shared ReadOnly Property GameIdPlain As String
            Get
                Return DecodeBase64(GameJolt_Game_ID)
            End Get
        End Property

        Public Shared ReadOnly Property GameKeyPlain As String
            Get
                Return DecodeBase64(GameJolt_Game_Key)
            End Get
        End Property

        ' === Private: Base64 decoder ===
        Private Shared Function DecodeBase64(ByVal encodedText As String) As String
            Try
                Dim data() As Byte = System.Convert.FromBase64String(encodedText)
                Return System.Text.Encoding.UTF8.GetString(data)
            Catch
                Return encodedText
            End Try
        End Function
    End Class
End Namespace