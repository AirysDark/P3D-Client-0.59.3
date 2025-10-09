Imports System.Diagnostics

' Lightweight logging shim used by APICall.vb (and anywhere else you like).
' Maps to your existing Logger.Log types if available; otherwise falls back to Debug.WriteLine.

Public NotInheritable Class ClientTrace
    Private Sub New()
    End Sub

    Public Shared Sub LogInfo(message As String)
        Try
            Logger.Log(Logger.LogTypes.Message, message)
        Catch
            Debug.WriteLine("[INFO] " & message)
        End Try
    End Sub

    Public Shared Sub LogWarn(message As String)
        Try
            Logger.Log(Logger.LogTypes.Warning, message)
        Catch
            Debug.WriteLine("[WARN] " & message)
        End Try
    End Sub

    Public Shared Sub LogError(message As String)
        Try
            Logger.Log(Logger.LogTypes.ErrorMessage, message)
        Catch
            Debug.WriteLine("[ERROR] " & message)
        End Try
    End Sub

    Public Shared Sub LogDebug(message As String)
        Try
            Logger.Log(Logger.LogTypes.Debug, message)
        Catch
            Debug.WriteLine("[DEBUG] " & message)
        End Try
    End Sub
End Class