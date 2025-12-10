using UnityEngine;

public class DeathManager
{
    private const string DEATH_KEY = "TotalDeaths";
    public int TotalDeaths { get; private set; }

    public DeathManager()
    {
        Load();
    }

    public void RegisterDeath()
    {
        TotalDeaths++;
        Save();
    }

    private void Save()
    {
        PlayerPrefs.SetInt(DEATH_KEY, TotalDeaths);
        PlayerPrefs.Save();
    }

    private void Load()
    {
        TotalDeaths = PlayerPrefs.GetInt(DEATH_KEY, 0);
    }
}