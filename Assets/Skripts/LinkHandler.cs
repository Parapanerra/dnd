using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LinkHandler : MonoBehaviour
{
    [SerializeField] private List<Button> linkButtons;
    [SerializeField] private List<string> urls;

    private void Start()
    {
        if (linkButtons == null || urls == null)
            return;

        for (int i = 0; i < linkButtons.Count; i++)
        {
            if (linkButtons[i] == null)
                continue;
            if (i >= urls.Count)
            {
                Debug.LogError("URL list is shorter than button list!");
                continue;
            }

            string url = urls[i];
            linkButtons[i].onClick.AddListener(() => OpenLink(url));
        }
    }

    private void OpenLink(string url)
    {
        if (!string.IsNullOrWhiteSpace(url))
            Application.OpenURL(url);
    }
}
