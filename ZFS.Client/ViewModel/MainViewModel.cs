using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZFS.Client.LogicCore.Common;
using ZFS.Client.LogicCore.Configuration;
using ZFS.Client.LogicCore.Enums;
using ZFS.Client.LogicCore.Interface;
using ZFS.Client.UiCore.Template.DemoCharts;

namespace ZFS.Client.ViewModel
{
    /// <summary>
    /// ��ҳ
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        #region ģ��ϵͳ

        private ModuleManager _ModuleManager;

        public ObservableCollection<PageInfo> OpenPageCollection { get; set; } = new ObservableCollection<PageInfo>();

        /// <summary>
        /// ģ�������
        /// </summary>
        public ModuleManager ModuleManager
        {
            get { return _ModuleManager; }
        }

        #endregion

        #region ������

        private PopBoxViewModel _PopBoxView;

        /// <summary>
        /// ��������
        /// </summary>
        public PopBoxViewModel PopBoxView
        {
            get { return _PopBoxView; }
        }

        private NoticeViewModel _NoticeView;

        /// <summary>
        /// ֪ͨģ��
        /// </summary>
        public NoticeViewModel NoticeView
        {
            get { return _NoticeView; }
        }

        #endregion

        #region ����(Binding Command)

        private object _CurrentPage;

        /// <summary>
        /// ��ǰѡ��ҳ
        /// </summary>
        public object CurrentPage
        {
            get { return _CurrentPage; }
            set { _CurrentPage = value; RaisePropertyChanged(); }
        }

        private RelayCommand<Module> _ExcuteCommand;
        private RelayCommand<PageInfo> _ExitCommand;
        private RelayCommand<ModuleGroup> _ExcuteGroupCommand;

        /// <summary>
        /// �򿪷���
        /// </summary>
        public RelayCommand<ModuleGroup> ExcuteGroupCommand
        {
            get
            {
                if (_ExcuteGroupCommand == null)
                {
                    _ExcuteGroupCommand = new RelayCommand<ModuleGroup>(t => ExcuteGroup(t));
                }
                return _ExcuteGroupCommand;
            }
            set { _ExcuteGroupCommand = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// ��ģ��
        /// </summary>
        public RelayCommand<Module> ExcuteCommand
        {
            get
            {
                if (_ExcuteCommand == null)
                {
                    _ExcuteCommand = new RelayCommand<Module>(t => Excute(t));
                }
                return _ExcuteCommand;
            }
            set { _ExcuteCommand = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// �ر�ҳ
        /// </summary>
        public RelayCommand<PageInfo> ExitCommand
        {
            get
            {
                if (_ExitCommand == null)
                {
                    _ExitCommand = new RelayCommand<PageInfo>(t => ExitPage(t));
                }
                return _ExitCommand;
            }
            set { _ExitCommand = value; RaisePropertyChanged(); }
        }

        #endregion

        #region ��ʼ��/ҳ�����

        /// <summary>
        /// ��ʼ����ҳ
        /// </summary>
        public async void InitDefaultView()
        {
            //��ʼ��������,֪ͨ����
            _PopBoxView = new PopBoxViewModel();
            _NoticeView = new NoticeViewModel();
            //���ش���ģ��
            _ModuleManager = new ModuleManager();
            await _ModuleManager.LoadModules();
            //����ϵͳĬ����ҳ
            var page = OpenPageCollection.FirstOrDefault(t => t.HeaderName.Equals("ϵͳ��ҳ"));
            if (page == null)
            {
                //��ʾDemo����Ĭ����ҳ,���������ܡ� ʵ�ʿ������Ƴ����߸��¿���������
                HomeAbout about = new HomeAbout();
                OpenPageCollection.Add(new PageInfo() { HeaderName = "ϵͳ��ҳ", Body = about });
                CurrentPage = OpenPageCollection[OpenPageCollection.Count - 1];
            }
        }

        public void ExcuteGroup(ModuleGroup group)
        {
            ModuleManager.Modules.Clear();
            foreach (var m in group.Modules)
                ModuleManager.Modules.Add(m);
            if (expansionState == ExpansionState.Open)
                expansionAction();
        }

        /// <summary>
        /// ִ��ģ��
        /// </summary>
        /// <param name="module"></param>
        private async void Excute(Module module)
        {
            try
            {
                var page = OpenPageCollection.FirstOrDefault(t => t.HeaderName.Equals(module.Name));
                if (page != null) { CurrentPage = page; return; }
                if (string.IsNullOrWhiteSpace(module.Code))
                {
                    //404ҳ��
                    //DefaultViewPage defaultViewPage = new DefaultViewPage();
                    //OpenPageCollection.Add(new PageInfo() { HeaderName = module.Name, Body = defaultViewPage });
                    //CurrentPage = defaultViewPage;
                }
                else
                {
                    expansionAction();//����
                    await Task.Factory.StartNew(() =>
                    {
                        var dialog = ServiceProvider.Instance.Get<IModel>(module.Code);
                        dialog.BindDefaultModel();
                        OpenPageCollection.Add(new PageInfo() { HeaderName = module.Name, Body = dialog.GetView() });
                    }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
                }
                CurrentPage = OpenPageCollection[OpenPageCollection.Count - 1];
            }
            catch (Exception ex)
            {
                Msg.Error(ex.Message);
            }
            finally
            {
                Messenger.Default.Send(false, "PackUp");
                GC.Collect();
            }
        }

        /// <summary>
        /// �ر�ҳ��
        /// </summary>
        /// <param name="module"></param>
        private void ExitPage(PageInfo module)
        {
            try
            {
                var tab = OpenPageCollection.FirstOrDefault(t => t.HeaderName.Equals(module.HeaderName));
                if (tab.HeaderName != "ϵͳ��ҳ") OpenPageCollection.Remove(tab);
            }
            catch (Exception ex)
            {
                Msg.Error(ex.Message);
            }
        }

        #endregion

        #region ��ҳUI_Command

        private ExpansionState expansionState = ExpansionState.Open;

        private RelayCommand expansionCommand;
        private RelayCommand<string> inputChangeCommand;

        public RelayCommand ExpansionCommand
        {
            get
            {
                if (expansionCommand == null)
                    expansionCommand = new RelayCommand(() => expansionAction());
                return expansionCommand;
            }
        }

        public RelayCommand<string> InputChangeCommand
        {
            get
            {
                if (inputChangeCommand == null)
                    inputChangeCommand = new RelayCommand<string>((t) => inputEvent(t));
                return inputChangeCommand;
            }
        }

        private void expansionAction()
        {
            bool v = expansionState == ExpansionState.Close;
            expansionState = v ? ExpansionState.Open : ExpansionState.Close;
            Messenger.Default.Send(expansionState, "expansionCommand");
        }

        public void inputEvent(string input)
        {
            ModuleManager.Modules.Clear();
            if (string.IsNullOrWhiteSpace(input)) return;
            var groups = ModuleManager.ModuleGroups.Select(t => t.Modules.Where(q => q.Name.Contains(input)).ToList()).ToList();
            if (groups != null)
            {
                groups.ForEach(arg =>
                {
                    arg.ForEach(args =>
                    {
                        ModuleManager.Modules.Add(args);
                    });
                });
            }
        }


        #endregion
    }
}