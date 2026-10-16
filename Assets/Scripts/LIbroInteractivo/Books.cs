using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;

public class book : MonoBehaviour
{
    class PageFaceGraphics
    {
        public Graphic[] front;
        public Graphic[] back;
    }

    [SerializeField] float pageSpeed = 0.5f;
    [SerializeField] List<Transform> pages;
    List<PageFaceGraphics> pageGraphics;
    readonly Dictionary<TMP_Text, Quaternion> backTextRotations = new Dictionary<TMP_Text, Quaternion>();
    int index = -1;
    bool rotate = false;
    [SerializeField] GameObject backButton;
    [SerializeField] GameObject forwardButton;
    [SerializeField] AudioSource pageTurnSource;
    [SerializeField] AudioClip pageTurnClip;

    private void Start()
    {
        InitialState();
    }

    public void InitialState()
    {
        pageGraphics = new List<PageFaceGraphics>(pages.Count);
        for (int i = 0; i < pages.Count; i++)
        {
            pages[i].rotation = Quaternion.identity;
            Graphic[] graphics = pages[i].GetComponentsInChildren<Graphic>(true);
            List<Graphic> frontGraphics = new List<Graphic>();
            List<Graphic> backGraphics = new List<Graphic>();
            foreach (Graphic graphic in graphics)
            {
                string objectName = graphic.gameObject.name;
                bool isBackward = objectName.IndexOf("backward", System.StringComparison.OrdinalIgnoreCase) >= 0;
                bool isForward = objectName.IndexOf("forward", System.StringComparison.OrdinalIgnoreCase) >= 0;
                TMP_Text text = graphic as TMP_Text;

                if (isBackward)
                {
                    if (text != null)
                    {
                        Quaternion originalRotation;
                        if (!backTextRotations.TryGetValue(text, out originalRotation))
                        {
                            originalRotation = text.transform.localRotation;
                            backTextRotations.Add(text, originalRotation);
                        }
                        text.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * originalRotation;
                    }
                    backGraphics.Add(graphic);
                }
                else if (isForward || text != null)
                {
                    frontGraphics.Add(graphic);
                }
            }

            pageGraphics.Add(new PageFaceGraphics
            {
                front = frontGraphics.ToArray(),
                back = backGraphics.ToArray()
            });
            SetPageFaceVisible(i, true);
        }
        pages[0].SetAsLastSibling();
        KeepNavigationButtonsOnTop();
        backButton.SetActive(false);

    }

    public void RotateForward()
    {
        if (rotate || pages == null || index + 1 >= pages.Count) { return; }
        index++;
        float angle = 180; //in order to rotate the page forward, you need to set the rotation by 180 degrees around the y axis
        ForwardButtonActions();
        pages[index].SetAsLastSibling();
        KeepNavigationButtonsOnTop();
        PlayPageTurnSound();
        StartCoroutine(Rotate(angle, true));

    }

    public void ForwardButtonActions()
    {
        if (backButton.activeInHierarchy == false)
        {
            backButton.SetActive(true); //every time we turn the page forward, the back button should be activated
        }
        if (index == pages.Count - 1)
        {
            forwardButton.SetActive(false); //if the page is last then we turn off the forward button
        }
    }

    public void RotateBack()
    {
        if (rotate || pages == null || index < 0 || index >= pages.Count) { return; }
        float angle = 0; //in order to rotate the page back, you need to set the rotation to 0 degrees around the y axis
        pages[index].SetAsLastSibling();
        KeepNavigationButtonsOnTop();
        BackButtonActions();
        PlayPageTurnSound();
        StartCoroutine(Rotate(angle, false));
    }

    void PlayPageTurnSound()
    {
        if (pageTurnSource != null && pageTurnSource.isActiveAndEnabled && pageTurnClip != null)
            pageTurnSource.PlayOneShot(pageTurnClip);
    }

    public void BackButtonActions()
    {
        if (forwardButton.activeInHierarchy == false)
        {
            forwardButton.SetActive(true); //every time we turn the page back, the forward button should be activated
        }
        if (index - 1 == -1)
        {
            backButton.SetActive(false); //if the page is first then we turn off the back button
        }
    }

    IEnumerator Rotate(float angle, bool forward)
    {
        float value = 0f;
        bool textHidden = !forward;
        while (true)
        {
            rotate = true;
            Quaternion targetRotation = Quaternion.Euler(0, angle, 0);
            value += Time.deltaTime * pageSpeed;
            pages[index].rotation = Quaternion.Slerp(pages[index].rotation, targetRotation, value); //smoothly turn the page
            float pageAngle = Quaternion.Angle(pages[index].rotation, Quaternion.identity);
            if (forward && !textHidden && pageAngle >= 90f)
            {
                SetPageFaceVisible(index, false);
                textHidden = true;
            }
            else if (forward == false && textHidden && pageAngle <= 90f)
            {
                SetPageFaceVisible(index, true);
                textHidden = false;
            }
            float angle1 = Quaternion.Angle(pages[index].rotation, targetRotation); //calculate the angle between the given angle of rotation and the current angle of rotation
            if (angle1 < 0.1f)
            {
                if (forward == false)
                {
                    index--;
                }
                rotate = false;
                break;

            }
            yield return null;

        }
    }

    void SetPageFaceVisible(int pageIndex, bool showFront)
    {
        foreach (Graphic graphic in pageGraphics[pageIndex].front)
        {
            graphic.enabled = showFront;
        }
        foreach (Graphic graphic in pageGraphics[pageIndex].back)
        {
            graphic.enabled = !showFront;
        }
    }

    void KeepNavigationButtonsOnTop()
    {
        backButton.transform.SetAsLastSibling();
        forwardButton.transform.SetAsLastSibling();
    }
}
