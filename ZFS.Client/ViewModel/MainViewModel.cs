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
        public MainViewModel()
        {
            //CommandParameter = "{Binding Path=DataContext,RelativeSource={RelativeSource Mode=FindAncestor,AncestorType={x:Type TabItem}}}"
        }

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

        public void ExitPage(MenuBehaviorType behaviorType, string pageName)
        {
            switch (behaviorType)
            {
                case MenuBehaviorType.ExitCurrentPage:
                    var page = OpenPageCollection.FirstOrDefault(t => t.HeaderName.Equals(pageName));
                    if (page.HeaderName != "ϵͳ��ҳ") OpenPageCollection.Remove(page);
                    break;
                case MenuBehaviorType.ExitAllPage:
                    var pageList = OpenPageCollection.Where(t => t.HeaderName != "ϵͳ��ҳ").ToList();
                    if (pageList != null)
                    {
                        pageList.ForEach(t =>
                        {
                            OpenPageCollection.Remove(t);
                        });
                    }
                    break;
                case MenuBehaviorType.ExitAllExcept:
                    var pageListExcept = OpenPageCollection.Where(t => t.HeaderName != pageName && t.HeaderName != "ϵͳ��ҳ").ToList();
                    if (pageListExcept != null)
                    {
                        pageListExcept.ForEach(t =>
                        {
                            OpenPageCollection.Remove(t);
                        });
                    }
                    break;
            }
        }

        #endregion

    }
}