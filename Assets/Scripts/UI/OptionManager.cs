using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class OptionManager : MonoBehaviour
{
    [SerializeField] private GameObject volume;
    [SerializeField] private GameObject keySetting;
    [SerializeField] private Slider sfxVolume;
    [SerializeField] private Slider bgmVolume;
    [SerializeField] private TMP_Dropdown languageDropDown;
    [SerializeField] private AudioClip buttonSound;

    private void Awake()
    {
        languageDropDown.value = SaveManager.Data.languageIndex;
        sfxVolume.value = SaveManager.Data.sfxVolume;
        bgmVolume.value = SaveManager.Data.bgmVolume;
        volume.SetActive(true);
        keySetting.SetActive(false);
    }

    public void OnLanguageChanged(int index)
    {
        var locales = LocalizationSettings.AvailableLocales.Locales;
        if (index < locales.Count)
            LocalizationSettings.SelectedLocale = locales[index];
        SaveManager.SetLanguage(index);
    }

    public void OnVolume()
    {
        volume.SetActive(true);
        keySetting.SetActive(false);
    }

    public void OnKeySetting()
    {
        volume.SetActive(false);
        keySetting.SetActive(true);
    }

    public void OnClose()
    {
        gameObject.SetActive(false);
        EventSystem.current.SetSelectedGameObject(null);
    }

    // SoundManager.SetSFXVolume / SetBGMVolume 이 내부적으로 SaveManager 저장까지 처리한다.
    public void OnSFXChange(float value) => SoundManager.Instance.SetSFXVolume(value);

    public void OnBGMChange(float value) => SoundManager.Instance.SetBGMVolume(value);

    public void OnClicked()
    {
        SoundManager.Instance.PlaySFX(buttonSound);
    }
}
