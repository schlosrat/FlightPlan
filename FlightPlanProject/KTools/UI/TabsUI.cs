using UnityEngine;

namespace FlightPlan.KTools.UI;

public interface IPageContent
{
    // Name drawn in the Tab Button
    public string Name
    {
        get;
    }

    // Icon drawn in the Tab Button
    public GUIContent Icon
    {
        get;
    }

    // if is IsRunning, UI is drawn lighted
    public bool IsRunning
    {
        get;
    }

    // if IsActive Tab is visible
    public bool IsActive
    {
        get;
    }

    // usefull to knows is current _page is visible (you can switch off not needed updates if not set)
    public bool UIVisible
    {
        get;
        set;
    }

    // Main Page UI called Here
    public void OnGUI();
}

public class TabsUI
{
    public List<IPageContent> Pages = new();

    private List<IPageContent> _filteredPages = new();

    IPageContent CurrentPage = null;

    // must be called after adding Pages
    private bool _tabButton(bool isCurrent, bool isActive, string txt, GUIContent icon)
    {
        GUIStyle _style = isActive ? KBaseStyle.TabActive : KBaseStyle.TabNormal;
        if (icon == null)
            return GUILayout.Toggle(isCurrent, txt, _style, GUILayout.ExpandWidth(true));
        else
            return GUILayout.Toggle(isCurrent, icon, _style, GUILayout.ExpandWidth(true));
    }

    List<float> TabsWidth = new();

    public int DrawTabs(int current, float maxWidth = 300)
    {
        current = GeneralTools.ClampInt(current, 0, _filteredPages.Count - 1);
        GUILayout.BeginHorizontal();

        int result = current;

        // compute sizes
        if (TabsWidth.Count != _filteredPages.Count)
        {
            TabsWidth.Clear();
            for (int index = 0; index < _filteredPages.Count; index++)
            {
                var page = _filteredPages[index];
                KBaseStyle.TabNormal.CalcMinMaxWidth(new GUIContent(page.Name, ""), out float _minWidth, out _);
                TabsWidth.Add(_minWidth);
            }
        }
        float _xPos = 0;

        for (int index = 0; index < _filteredPages.Count; index++)
        {
            IPageContent _page = _filteredPages[index];

            float _width = TabsWidth[index];

            if (_xPos > maxWidth)
            {
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                _xPos = 0;
            }
            _xPos += _width;

            bool _isCurrent = current == index;
            if (_tabButton(_isCurrent, _page.IsRunning, _page.Name, _page.Icon))
            {
                if (!_isCurrent)

                    result = index;
            }
        }

      /*  if (_xPos < _maxWidth * 0.7f)
        {
            GUILayout.FlexibleSpace();
        }*/
        GUILayout.EndHorizontal();

        UI_Tools.Separator();
        return result;
    }

    public void Init()
    {
        CurrentPage = Pages[KBaseSettings.MainTabIndex];
        CurrentPage.UIVisible = true;
    }

    // must be called to rebuild the _filteredPages list 
    public void Update()
    {
        _filteredPages = new List<IPageContent>();
        for (int index = 0; index < Pages.Count; index++)
        {
            if (Pages[index].IsActive)
                _filteredPages.Add(Pages[index]);
        }
    }

    public void OnGUI()
    {
        int _currentIndex = KBaseSettings.MainTabIndex;

        if (_filteredPages.Count == 0 )
        {
            UI_Tools.Error("NO active Tab tage !!!");
            return;
        }
        int _result;
        if (_filteredPages.Count == 1)
        {
            _result = 0;
        }
        else
        {
            _result = DrawTabs(_currentIndex);
        }
        
        _result = GeneralTools.ClampInt(_result, 0, _filteredPages.Count - 1);
        IPageContent _page = _filteredPages[_result];

        if (_page != CurrentPage)
        {
            CurrentPage.UIVisible = false;
            //KBaseSettings.MainTabIndex = _result;
            //CurrentPage = _filteredPages[_result];
            CurrentPage = _page;
            CurrentPage.UIVisible = true;
        }

        KBaseSettings.MainTabIndex = _result;

        CurrentPage.OnGUI();
    }
}
