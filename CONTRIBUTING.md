# Contributing to DJI Waypoint Manager

Thank you for your interest in contributing to DJI Waypoint Manager! We welcome contributions from the community and are pleased to have them.

## 🚀 Getting Started

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) or [Visual Studio Code](https://code.visualstudio.com/)
- [Git](https://git-scm.com/)
- Basic understanding of C#, WPF, and JavaScript

### Setting Up Development Environment

1. **Fork the repository** on GitHub
2. **Clone your fork** locally:
   ```bash
   git clone https://github.com/YOUR_USERNAME/DjiWaypointManager.git
   cd DjiWaypointManager
   ```
3. **Add upstream remote**:
   ```bash
   git remote add upstream https://github.com/smbakker/DjiWaypointManager.git
   ```
4. **Install dependencies**:
   ```bash
   cd src
   dotnet restore
   ```
5. **Build and test**:
   ```bash
   dotnet build
   dotnet run --project DjiWaypointManager
   ```

## 📝 How to Contribute

### Reporting Bugs

Before creating bug reports, please check the existing issues to avoid duplicates. When creating a bug report, include:

- **Clear description** of the problem
- **Steps to reproduce** the issue
- **Expected vs actual behavior**
- **System information** (Windows version, .NET version)
- **Screenshots or logs** if applicable
- **DJI device information** if relevant

### Suggesting Features

Feature requests are welcome! Please:

- **Check existing issues** for similar requests
- **Provide clear use cases** for the feature
- **Explain the benefit** to users
- **Consider implementation complexity**

### Code Contributions

#### Types of Contributions We Welcome

- 🐛 **Bug fixes**
- ✨ **New features**
- 📚 **Documentation improvements**
- 🎨 **UI/UX enhancements**
- 🚀 **Performance optimizations**
- 🧪 **Test coverage improvements**
- 🔧 **Code refactoring**

#### Development Workflow

1. **Create a branch** for your work:
   ```bash
   git checkout -b feature/your-feature-name
   # or
   git checkout -b bugfix/issue-description
   ```

2. **Make your changes** following our coding standards (see below)

3. **Test your changes** thoroughly:
   - Build the solution without errors
   - Test the UI functionality
   - Verify map interactions work correctly
   - Test with DJI device if applicable

4. **Commit your changes**:
   ```bash
   git add .
   git commit -m "Add descriptive commit message"
   ```

5. **Push to your fork**:
   ```bash
   git push origin feature/your-feature-name
   ```

6. **Create a Pull Request** on GitHub

## 📋 Coding Standards

### C# Code Style

- Follow [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use **PascalCase** for public members
- Use **camelCase** for private fields and local variables
- Use **meaningful names** for variables and methods
- Add **XML documentation** for public APIs
- Keep methods **focused and small**
- Use **async/await** for asynchronous operations

### XAML Guidelines

- Use proper **indentation** (4 spaces)
- Organize properties **logically**
- Use **data binding** where appropriate
- Follow **MVVM patterns** when possible

### JavaScript/HTML

- Use **consistent indentation** (2 spaces)
- Follow **modern JavaScript practices**
- Document **complex functions**
- Ensure **cross-browser compatibility**

### Git Commit Messages

- Use the **imperative mood** ("Add feature" not "Added feature")
- Keep the **first line under 50 characters**
- Reference **issue numbers** when applicable
- Examples:
  - `Add waypoint altitude validation`
  - `Fix map rendering issue #123`
  - `Update README installation instructions`

## 🧪 Testing Guidelines

### Manual Testing

- Test on **Windows 10 and 11**
- Verify **map functionality** works correctly
- Test **waypoint creation and editing**
- Check **device detection** if you have DJI hardware
- Validate **import/export** functionality

### Areas Needing Tests

We welcome contributions for:
- **Unit tests** for business logic
- **Integration tests** for device detection
- **UI automation tests**
- **Performance tests** for large missions

## 📁 Project Structure

Understanding the codebase:

```
src/DjiWaypointManager/
├── Models/          # Data models and DTOs
├── Services/        # Business logic and device handling
├── Views/           # XAML UI files
├── ViewModels/      # MVVM view models (if added)
├── js/              # JavaScript for map functionality
└── Resources/       # Images, styles, etc.
```

## 🔄 Pull Request Process

1. **Ensure your PR**:
   - Has a clear title and description
   - References related issues
   - Includes screenshots for UI changes
   - Passes all builds
   - Follows coding standards

2. **PR Review Process**:
   - Maintainers will review within 1-2 weeks
   - Address feedback promptly
   - Keep discussions professional and constructive

3. **After Approval**:
   - PRs will be merged by maintainers
   - Your contribution will be credited

## 🎯 Priority Areas

We're particularly interested in contributions for:

- **Cross-platform support** (exploring Avalonia or MAUI)
- **Additional DJI device support**
- **Enhanced mission validation**
- **Performance improvements**
- **Better error handling**
- **Internationalization (i18n)**
- **Dark theme support**

## 💬 Communication

- **GitHub Issues**: For bugs and feature requests
- **GitHub Discussions**: For questions and general discussion
- **Pull Requests**: For code review and collaboration

## 🤝 Code of Conduct

This project follows our [Code of Conduct](CODE_OF_CONDUCT.md). Please read it before contributing.

## 📄 License

By contributing to DJI Waypoint Manager, you agree that your contributions will be licensed under the MIT License.

---

Thank you for helping make DJI Waypoint Manager better! 🚁✨