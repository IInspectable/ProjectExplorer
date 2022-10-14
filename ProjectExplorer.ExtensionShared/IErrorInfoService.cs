using System;

namespace IInspectable.ProjectExplorer.Extension; 

interface IErrorInfoService {

    void ShowErrorInfoBar(Exception ex);
    void RemoveErrorInfoBar();
}