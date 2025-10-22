﻿## Code coverage template

This document outlines the structure and steps to set up a CI/CD pipeline for code coverage for GitHub actions.

### Workflow Structure
This is the suggested directory structure for the workflow file:
```
.github/workflows/
├── ci-code-coverage.yml   # CI workflow for code coverage
```

### Template to follow

This is a template for a GitHub Actions workflow that performs code coverage analysis for .NET projects. It checks out the code, sets up the .NET environment, downloads test results, combines coverage reports, and publishes the results.

```yaml
name: Code Coverage

on:
  workflow_call:

permissions:
  contents: read

jobs:
  publish-coverage:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@08c6903cd8c0fde910a37f88322edcfb5dd907a8 # v5.0.0
      - name: Setup .NET
        uses: actions/setup-dotnet@d4c94342e560b34958eacfc5d055d21461ed1c5d # v5.0.0
        with:
          dotnet-version: |
            10.0.100-rc.1.25451.107
            9.0.x

      - name: Download test results
        uses: actions/download-artifact@634f93cb2916e3fdff6788551b99b062d0335ce0 # v5.0.0
        with:
          pattern: testresults-*

      - name: Combine coverage reports
        uses: danielpalme/ReportGenerator-GitHub-Action@5.4.17
        with:
          reports: "**/*.cobertura.xml"
          targetdir: "${{ github.workspace }}/report"
          reporttypes: "HtmlSummary;Cobertura;MarkdownSummary;MarkdownSummaryGithub"
          verbosity: "Info"
          title: "Code Coverage"
          tag: "${{ github.run_number }}_${{ github.run_id }}"
          customSettings: ""
          toolpath: "reportgeneratortool"

      - name: Upload combined coverage XML
        uses: actions/upload-artifact@ea165f8d65b6e75b540449e92b4886f43607fa02 # v4.6.2
        with:
          name: coverage
          path: ${{ github.workspace }}/report
          retention-days: 7

      - name: Publish code coverage report
        uses: irongut/CodeCoverageSummary@51cc3a756ddcd398d447c044c02cb6aa83fdae95 # v1.3.0
        with:
          filename: "report/Cobertura.xml"
          badge: true
          fail_below_min: true
          format: markdown
          hide_branch_rate: false
          hide_complexity: false
          indicators: true
          output: both
          thresholds: "60 80"

      - name: Upload combined coverage markdown
        uses: actions/upload-artifact@ea165f8d65b6e75b540449e92b4886f43607fa02 # v4.6.2
        with:
          name: coverage-markdown
          path: ${{ github.workspace }}/code-coverage-results.md
          retention-days: 7

      - name: Coverage on step summary
        if: always()
        run: cat "${{ github.workspace }}/report/SummaryGithub.md" >> $GITHUB_STEP_SUMMARY
```

