namespace EasySave.Core;

public enum LogFormat { Json, Xml }
public enum BackupType { Full, Differential }
public enum JobState { Inactive, Active, Paused, Waiting }

public enum LogTarget { Local, Remote, Both}
//On définit une fois pour toutes les formats de logs, les types de backup et les états du job