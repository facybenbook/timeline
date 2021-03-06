﻿using System.IO;
using UnityEngine.Playables;

namespace UnityEngine.Timeline
{
    public class TimelineLoader
    {

        private XDirectorAsset asset;

        public void Load(string path, PlayableDirector director)
        {
            FileStream stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Read);
            BinaryReader br = new BinaryReader(stream);
            OnPreLoad(director);
            asset = Load(br, director);
            OnPostLoad(director);
            br.Close();
            stream.Close();
        }


        private void OnPreLoad(PlayableDirector director)
        {
            DirectorSystem.Director = director;
            director.enabled = false;
            var playableAsset = director.playableAsset;
            if (playableAsset)
            {
                foreach (PlayableBinding pb in director.playableAsset.outputs)
                {
                    director.ClearGenericBinding(pb.sourceObject);
                }
            }
            director.time = 0;
            director.Stop();
        }

        private void OnPostLoad(PlayableDirector director)
        {
            TimelineUtil.playMode = TimelinePlayMode.RUNPLAYING;
            var tracks = asset.TrackAssets;
            for (int i = 0; i < tracks.Length; i++)
            {
                tracks[i].OnPostLoad(asset);
            }

            asset.name = director.name;
            asset.SetSpeed(1);
            director.playableAsset = asset;
            director.enabled = true;
        }


        private XDirectorAsset Load(BinaryReader reader, PlayableDirector director)
        {
            var asset = ScriptableObject.CreateInstance<XDirectorAsset>();
            var dur = reader.ReadDouble();
            asset.SetDuration(dur);
            int cnt = reader.ReadInt32();
            asset.TrackAssets = new XTrackAsset[cnt];
            for (int i = 0; i < cnt; i++)
            {
                LoadTrack(reader, ref asset.TrackAssets[i], director);
            }
            return asset;
        }


        private void LoadTrack(BinaryReader reader, ref XTrackAsset track, PlayableDirector director)
        {
            var type = (TrackType)reader.ReadInt32();
            track = DirectorSystem.CreateTrack(type);
            track.Load(reader, type);

            string bind = reader.ReadString();
            if (!string.IsNullOrEmpty(bind))
            {
                GameObject bindGo = GameObject.Find(bind);
                director.SetGenericBinding(track, bindGo);
                track.bindObj = bindGo;
            }
            DirectorSystem.ReadTrackInfo(reader, track, type);

            //clips
            int cnt = reader.ReadInt32();
            for (int i = 0; i < cnt; i++)
            {
                LoadClip(reader, track);
            }

            //markers
            cnt = reader.ReadInt32();
            for (int i = 0; i < cnt; i++)
            {
                LoadMarker(reader, track);
            }
        }


        private void LoadClip(BinaryReader reader, XTrackAsset track)
        {
            PlayableAsset asset = DirectorSystem.CreateClipAsset(track.trackType);
            TimelineClip clip = track.CreateClip(asset);
            clip.start = reader.ReadDouble();
            clip.clipIn = reader.ReadDouble();
            clip.duration = reader.ReadDouble();
            clip.timeScale = reader.ReadDouble();
            clip.blendInDuration = reader.ReadDouble();
            clip.blendOutDuration = reader.ReadDouble();
            clip.easeInDuration = reader.ReadDouble();
            clip.easeOutDuration = reader.ReadDouble();
            if (asset is IDirectorIO)
            {
                var io = asset as IDirectorIO;
                io.Load(reader, track);
            }
            if (asset is ClipPlayaleAsset)
            {
                var cAsset = asset as ClipPlayaleAsset;
                cAsset.SetBind(track.bindObj);
            }
        }


        private void LoadMarker(BinaryReader reader, XTrackAsset track)
        {
            double time = reader.ReadDouble();
            int type = reader.ReadInt32();
            int parent = reader.ReadInt32();
            Marker marker = DirectorSystem.CreateMarker((MarkType)type);
            marker.time = time;
            if (marker is IDirectorIO)
            {
                var io = marker as IDirectorIO;
                io.Load(reader, track);
            }
            if (marker)
            {
                (marker as IMarker).Initialize(track);
                track.AddMarker(marker);
            }
        }

    }

}