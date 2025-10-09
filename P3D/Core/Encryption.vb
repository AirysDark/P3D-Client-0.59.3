Imports System.Security.Cryptography
Imports System.Text
Imports System.IO

Public Class Encryption

    ' ---------------- Convenience overloads (default to Classified.Encryption_Password) ----------------
    Public Shared Function EncryptString(ByVal s As String) As String
        Return EncryptString(s, GameJolt.Classified.Encryption_Password)
    End Function

    Public Shared Function DecryptString(ByVal s As String) As String
        Return DecryptString(s, GameJolt.Classified.Encryption_Password)
    End Function
    ' ---------------------------------------------------------------------------------------------------

    Public Shared Function EncryptString(ByVal s As String, ByVal password As String) As String
        If s Is Nothing Then s = String.Empty
        If password Is Nothing Then password = String.Empty

        ' Derive key (MD5 of deobfuscated password)
        Dim key() As Byte
        Using md5 As New MD5CryptoServiceProvider()
            key = md5.ComputeHash(Encoding.UTF8.GetBytes(StringObfuscation.DeObfuscate(password)))
        End Using

        Using rd As New RijndaelManaged()
            rd.Key = key
            rd.GenerateIV()

            Using ms As New MemoryStream()
                ' write IV first
                Dim iv() As Byte = rd.IV
                ms.Write(iv, 0, iv.Length)

                Using cs As New CryptoStream(ms, rd.CreateEncryptor(), CryptoStreamMode.Write)
                    Dim data() As Byte = Encoding.UTF8.GetBytes(s)
                    cs.Write(data, 0, data.Length)
                    cs.FlushFinalBlock()
                End Using

                Dim encdata() As Byte = ms.ToArray()
                Return Convert.ToBase64String(encdata)
            End Using
        End Using
    End Function

    Public Shared Function DecryptString(ByVal s As String, ByVal password As String) As String
        If String.IsNullOrEmpty(s) Then Return String.Empty
        If password Is Nothing Then password = String.Empty

        ' Derive key (MD5 of deobfuscated password)
        Dim key() As Byte
        Using md5 As New MD5CryptoServiceProvider()
            key = md5.ComputeHash(Encoding.UTF8.GetBytes(StringObfuscation.DeObfuscate(password)))
        End Using

        Dim encdata() As Byte = Convert.FromBase64String(s)
        Const rijndaelIvLength As Integer = 16
        If encdata.Length < rijndaelIvLength Then
            ' Corrupt payload
            Return String.Empty
        End If

        Using rd As New RijndaelManaged()
            ' read IV from start of buffer
            Dim iv(rijndaelIvLength - 1) As Byte
            Buffer.BlockCopy(encdata, 0, iv, 0, rijndaelIvLength)
            rd.IV = iv
            rd.Key = key

            ' remaining payload after IV
            Dim payloadLen As Integer = encdata.Length - rijndaelIvLength
            Using ms As New MemoryStream(encdata, rijndaelIvLength, payloadLen)
                Using cs As New CryptoStream(ms, rd.CreateDecryptor(), CryptoStreamMode.Read)
                    ' read all decrypted bytes
                    Using outMs As New MemoryStream()
                        Dim buf(4095) As Byte
                        Dim read As Integer
                        Do
                            read = cs.Read(buf, 0, buf.Length)
                            If read <= 0 Then Exit Do
                            outMs.Write(buf, 0, read)
                        Loop
                        Return Encoding.UTF8.GetString(outMs.ToArray())
                    End Using
                End Using
            End Using
        End Using
    End Function

End Class