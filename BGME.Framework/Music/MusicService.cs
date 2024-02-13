using BGME.Framework.Interfaces;
using PersonaMusicScript.Library;
using PersonaMusicScript.Types;
using PersonaMusicScript.Types.Music;
using PersonaMusicScript.Types.MusicCollections;

namespace BGME.Framework.Music;

public class MusicService
{
    private readonly MusicParser parser;
    private readonly MusicResources resources;
    private readonly IFileBuilder? fileBuilder;
    private readonly IBgmeApi bgmeApi;
    private readonly bool hotReload;

    private MusicSource currentMusic;

    public MusicService(
        MusicResources resources,
        IBgmeApi musicScripts,
        IFileBuilder? fileBuilder = null,
        bool hotReload = false)
    {
        this.resources = resources;
        this.bgmeApi = musicScripts;
        this.fileBuilder = fileBuilder;
        this.hotReload = hotReload;

        this.parser = new(resources);
        this.currentMusic = new(resources);

        this.OnMusicScriptsChanged(this.bgmeApi.GetMusicScripts());
        this.bgmeApi.MusicScriptsChanged += this.OnMusicScriptsChanged;
    }

    private void OnMusicScriptsChanged(string[] newMusicScripts)
    {
        this.currentMusic = new(resources);
        foreach (var musicScript in newMusicScripts)
        {
            try
            {
                this.parser.Parse(musicScript, this.currentMusic);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to parse music script.");
            }
        }

        if (this.hotReload && this.fileBuilder != null)
        {
            try
            {
                this.fileBuilder.Build(this);
                Log.Information("Built files.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to build files.");
            }
        }

        Log.Information("Music loaded.");
    }

    public EncounterMusic Encounters => this.currentMusic.Encounters;

    public FloorMusic Floors => this.currentMusic.Floors;

    public GlobalMusic Global => this.currentMusic.Global;

    public EventMusic Events => this.currentMusic.Events;

    public FrameTable? GetEventFrame(int majorId, int minorId, PmdType pmdType) => this.Events.GetEventFrame(majorId, minorId, pmdType);
}
