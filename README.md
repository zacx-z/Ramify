# Ramify MVVM Framework

A lightweight Model-View-ViewModel (MVVM) framework for Unity with a powerful binding system.

## Overview

Ramify is a Unity package that provides a robust MVVM architecture implementation with dependency injection and reactive bindings. It allows you to create modularized HUDs and UIs with custom view models and views. While it does not faciliate complex UI interactions, it can be used together with other UI frameworks for a complete solution.

Ramify is inspired by MVVM architecture and [Zenject](https://github.com/modesttree/Zenject). For full documentation, check this [link](http://nelasystem.com/lab/Ramify).

## Features

- **View-ViewModel Pattern**: Views only know about view models, and view models update views.
- **Hierarchical View Management**: Views can have child views that are bound to child view models.
- **Dependency Injection**: Bind view models to a hierarchy of views via dependency injection.
- **Reactive Binding**: Allow view models to be rebound when data changes.
- **View Model Composition/Decomposition**: Allow views to build new view models with data from other view models, which are then rebound to child views.

## Installation

### Manual Installation

1. Clone this repository
2. Copy the Ramify folder to your Unity project's Packages directory

## Basic Usage

### 1. Create a ViewModel

All view models must implement the `IViewModel` interface.

```csharp
using Nela.Ramify;
using System;

public interface IPlayerViewModel : IViewModel
{
    string PlayerName { get; }
    int Score { get; }
    event Action<int> onScoreChanged;
}

public class PlayerViewModel : IPlayerViewModel
{
    private int _score = 0;
    
    public string PlayerName { get; set; } = "Player";
    
    public int Score 
    { 
        get => _score; 
        set 
        { 
            if (_score != value)
            {
                _score = value;
                onScoreChanged?.Invoke(_score);
            }
        } 
    }
    
    public event Action<int> onScoreChanged;
    
    public void IncreaseScore(int amount)
    {
        Score += amount;
    }
}
```

### 2. Create a View

```csharp
using UnityEngine;
using UnityEngine.UI;
using Nela.Ramify;

public class PlayerView : View<IPlayerViewModel>
{
    [SerializeField] private Text playerNameText;
    [SerializeField] private Text scoreText;

    protected override void OnViewModelInit()
    {
        // Update UI with initial values
        UpdateUI();
        
        // Subscribe to score changes
        viewModel.onScoreChanged += OnScoreChanged;
    }

    private void OnScoreChanged(int newScore)
    {
        // Update just the score text when it changes
        scoreText.text = newScore.ToString();
    }

    private void UpdateUI()
    {
        playerNameText.text = viewModel.PlayerName;
        scoreText.text = viewModel.Score.ToString();
    }

    // Called when the view model changes
    protected override void OnViewModelDisposed()
    {
        // Clean up any subscriptions or resources
        viewModel.onScoreChanged -= OnScoreChanged;
    }
}
```

### 3. Bind ViewModel to View

In your class which is responsible for binding views (_Binder_), you can bind the view model to the view.

```csharp
// Create a view model
var playerViewModel = new PlayerViewModel(yourPlayer);

// Instantiate the view
var playerView = Instantiate(yourPlayerViewPrefab);

// Bind the view model to the view
playerView.Bind(playerViewModel);
```

You can bind multiple view models with `View.BindMultiple(viewModel1, viewModel2, ...)`, which is necessary when you have multiple views on one game object or when you have child views under the view you bind to. Alternatively, `View.BindAll(gameObject, viewModel1, viewModel2, ...)` will find all the view models as components on `gameObject` and bind them to the target view.

### List ViewModels

```csharp
public class PlayersListViewModel : IListViewModel<IPlayerViewModel>
{
    private List<IPlayerViewModel> _players = new List<IPlayerViewModel>();
    
    public IReadOnlyList<IPlayerViewModel> childViewModels => _players;
    
    public void PlayerListViewModel(IPlayerViewModel[] players)
    {
        foreach (var player in players) {
            _players.Add(player);
        }
    }
}
```

## Advanced Features

It is encouraged to create nested views and reuse child views. The _Binder_ is only responsible to bind the view models to the root of the views, and all the child views will be automatically bound (in the `OnInject()` method) with the view models either given by the _Binder_ or generated from one of the parent views.

### Child View Model Binding

You can bind child view models in the `OnInject()` method, which is the standard design pattern:

```csharp
protected override void OnInject(DIContainer diContainer)
{
    base.OnInject(diContainer);

    // Bind child view model for child views to use
    diContainer.Bind(viewModel.childViewModel);

    // Or you can create a view model
    diContainer.Bind(new ImageViewModel(viewModel.icon));
}
```

### Container Views for Lists

For handling lists of items, use the `ContainerView` class, which automatically handles child view model bindings:

```csharp
public class PlayersContainerView : ContainerView<PlayersListViewModel, PlayerViewModel>
{
    [SerializeField] private PlayerView playerViewPrefab;
    [SerializeField] private Transform contentParent;
    
    protected override void OnViewModelInit()
    {
        base.OnViewModelInit();
        
        // Clear existing views
        foreach (Transform child in contentParent) {
            Destroy(child.gameObject);
        }
        
        // Create a view for each view model
        foreach (var playerViewModel in viewModel.childViewModels) {
            var playerView = InstantiateView(playerViewPrefab, contentParent);
        }
    }
}
```

### Dynamic Rebinding

You can change your bound values at runtime using the `Rebind()` function.

```csharp
public class CurrentItemView : View<IPlayerStateViewModel>
{
    private IBinding<IItemViewModel> binding;

    protected override void OnInject(DIContainer diContainer)
    {
        // Create a binding reference for the view model
        binding = diContainer.CreateBinding<IItemViewModel>();

        base.OnInject(diContainer);
    }
    
    protected override void OnViewModelInit()
    {
        base.OnViewModelInit();
        binding.Rebind(viewModel.currentItem);

        viewModel.onCurrentItemChanged += binding.Rebind;
    }

    protected override void OnViewModelDisposed()
    {
        base.OnViewModelDisposed();
        viewModel.onCurrentItemChanged -= binding.Rebind;
    }
}
```

When the binding is rebound, all views using that binding will automatically be reinitialized with the new view model, that is, the `OnViewModelDisposed` and `OnViewModelInit` methods will be called in order.

### Editor Integration

Ramify includes editor tools to help debug and visualize bindings:

- **View Inspector**: Show the state of a view and its view model.
- **View Tree Debugger**: See the view tree outline and flow of view models through your views.

## License

MIT License
