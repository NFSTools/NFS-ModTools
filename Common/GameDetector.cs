﻿using System;
using System.IO;

namespace Common
{
    public static class GameDetector
    {
        public enum Game
        {
            MostWanted,
            Carbon,
            ProStreet,
            World,
            Unknown
        }

        /// <summary>
        /// Attempt to determine the NFS game installed in the given directory.
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public static Game DetectGame(string directory)
        {
            // speed.exe can be UG1 or MW
            if (File.Exists(Path.Combine(directory, "speed.exe")))
            {
                var tracksPath = Path.Combine(directory, "TRACKS");
                if (!Directory.Exists(tracksPath))
                {
                    throw new ArgumentException("TRACKS folder does not exist! Cannot determine game.");
                }

                if (File.Exists(Path.Combine(tracksPath, "L2RA.BUN"))
                    && File.Exists(Path.Combine(tracksPath, "STREAML2RA.BUN")))
                {
                    return Game.MostWanted;
                }
            }

            if (File.Exists(Path.Combine(directory, "nfsc.exe")))
            {
                return Game.Carbon;
            }

            if (File.Exists(Path.Combine(directory, "nfs.exe")))
            {
                return Game.ProStreet;
            }

            return File.Exists(Path.Combine(directory, "nfsw.exe")) ? Game.World : Game.Unknown;
        }
    }
}
