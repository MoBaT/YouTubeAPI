Public Class frmCaptcha
    Dim link As String

    Private Sub frmCaptcha_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        TextBox1.Focus()
    End Sub

    Private Sub TextBox1_TextChanged(ByVal sender As System.Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles TextBox1.KeyUp
        If e.KeyCode = Windows.Forms.Keys.Enter Then
            Button1.PerformClick()
        End If
    End Sub

    Public Overloads Function ShowDialog(ByVal _link As String) As String
        link = _link
        loadCaptcha()
        Me.ShowDialog()
        Return TextBox1.Text
    End Function

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        Me.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.Close()
    End Sub

    Private Sub loadCaptcha()
        Dim req As Net.HttpWebRequest = DirectCast(Net.HttpWebRequest.Create(link), Net.HttpWebRequest)
        Dim res As Net.HttpWebResponse = DirectCast(req.GetResponse, Net.HttpWebResponse)
        Dim img As Drawing.Image = Drawing.Image.FromStream(res.GetResponseStream)
        res.Close()
        PictureBox1.SizeMode = Windows.Forms.PictureBoxSizeMode.StretchImage
        PictureBox1.Image = img
    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        Me.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.Close()
    End Sub
End Class