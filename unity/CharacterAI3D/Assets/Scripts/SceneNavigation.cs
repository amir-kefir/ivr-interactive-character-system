using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNavigation : MonoBehaviour
{
    public void OpenMain()
    {
        SceneManager.LoadScene("MainScene");
    }

    public void OpenDialog()
    {
        SceneManager.LoadScene("DialogScene");
    }

    public void Logout()
    {
        SceneManager.LoadScene("LoginScene");
    }
}