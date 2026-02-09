using EasyLibrary.Models;
using System;

public interface IVue
{
    void MaximumJobLimitReached();

    void JobExecutionError(BackUpJob job);
    void JobExecutionError(BackUpJob job, Exception ex);
    void JobSucceeded(BackUpJob job);
    void AfficheMenuPrincipal();
}
