# Contributing

Before opening an issue or submitting a pull request, please read through this
document to familiarize yourself with the development process of the Flare
project.

## Issue Tracker

The [issue tracker](https://github.com/flare-lang/flare/issues) is where all bug
reports and feature requests should be posted. Other community areas are not
appropriate for these as issues raised there are likely to be lost or forgotten.

Please respect the following points when posting on the issue tracker:

* Do not post support requests or questions. Look [here](SUPPORT.md) to find
  better channels for these.
* Do not violate
  [GitHub's Acceptable Use terms](https://help.github.com/articles/github-terms-of-service#c-acceptable-use).
* Do not post off-topic comments or otherwise derail a discussion.

Issues and/or comments violating these rules may be deleted. Repeated violations
may lead to
[interaction limits](https://docs.github.com/en/github/building-a-strong-community/limiting-interactions-in-your-organization)
being enacted.

### Labels

We use labels to organize issues on the GitHub issue tracker. A full list of
labels can be found [here](https://github.com/flare-lang/flare/labels), along
with descriptions of each. Here are a few guidelines for how issue labels are to
be applied:

* An issue should have exactly one `type` and `state` label.
    * The `type` label should never change throughout an issue's lifetime.
    * The `state` label is expected to change multiple times as the issue
      progresses.
* An issue may have zero or more `note` labels depending on its `state` label.
* An issue should have at least one `area` label.
    * Labels of this type can be removed to correct inaccuracies, but they
      should not be removed if an issue is being addressed incrementally in
      different areas; instead, comment on the issue to indicate that a
      particular area has been addressed.
* An issue may have zero or more `cpu` and `os` labels as needed.
    * These can be left off if platform is highly likely to be an irrelevant
      factor for the issue. For example, issues with the compiler are unlikely
      to be platform-specific.
    * Labels of these types can be removed to correct inaccuracies, but they
      should not be removed if an issue is being addressed incrementally on
      specific platforms; instead, comment on the issue to indicate that a
      particular platform has been addressed.

### Milestones

We use milestones to plan future versions of Flare. When we triage an issue, we
will assign a milestone based on a rough guesstimation of the difficulty and
urgency of the issue, as well as other factors (such as whether fixing the issue
constitutes a breaking change). It is normal for an issue to have its milestone
changed if the initial guesstimation turned out to be inaccurate (in either
direction).

A full list of milestones can be found
[here](https://github.com/flare-lang/flare/milestones).

### Bug Reports

The ideal bug report is one that is immediately actionable by a person looking
to resolve it. To that end, a bug report should include:

* The output of `flare -v` (which contains Flare version, OS version, CPU
  architecture, etc).
* A self-contained test case that reproduces the issue, along with instructions
  on building and running it.
* A detailed description of current behavior (including stack traces(s) if
  applicable) and expected behavior.

Basically, try your best to ensure that fixing the bug will not require us to
ask you for further information. This makes it more likely for the bug to be
fixed in a timely fashion. Of course, in some cases, clarification and
discussion is necessary due to unexpected factors or sheer complexity.

### Feature Requests

Feature requests are welcome, but please note the following:

* Take a moment to consider whether the feature really makes sense for the core
  Flare project. The vast majority of features can live as community packages.
* It is up to you to convince the Flare development team that the feature should
  be included. You will need to make a strong case as the bar for new features
  is fairly high.
* Be as detailed as possible when describing the proposed feature. Explore
  benefits and drawbacks. Consider which alternatives are available, and explain
  why you believe they are insufficient.

At the end of the day, whether a feature request is accepted is up to the Flare
development team. There is no guarantee that a feature request will be accepted,
no matter how well-specified it is. Also, the Flare development team does not
commit to implementing a feature request when accepting it; acceptance just
means that anyone can implement it and submit a pull request in the knowledge
that the feature is a welcome addition.

## Pull Requests

[Pull requests](https://github.com/flare-lang/flare/pulls) are a great way to
contribute, whether it be code or documentation improvements. It is a good idea
to check the issue tracker to see if someone else is already working on
something before you start work on it. For feature additions, it is also
important to open a feature request on the issue tracker to see if the Flare
development team is actually interested in merging it.

If your pull request fixes one or more issues, please include triggers in the
relevant commit messages. See the
[GitHub documentation](https://help.github.com/articles/closing-issues-using-keywords)
for more information.

### Etiquette

While pull requests to clean up messy/hacky code (i.e. refactoring) are welcome,
we are not likely to accept pull requests that just change code style. Further,
please make sure that any code you contribute adheres to the style of the
surrounding code. The [EditorConfig](../.editorconfig) file in the project
should be helpful for this, as should the `dotnet format` command.

Please try to keep your pull request free of unrelated changes. If you need to
fix another bug in order to make your pull request work, please submit that bug
fix as a separate pull request.

### Licensing & Copyright

When you contribute code or documentation, you agree to make it available under
the terms of [Flare's license](../LICENSE.md).

Copyright assignment is not necessary when contributing to Flare. Individual
contributors retain copyrights for their contributed code.

### Inactivity

If a pull request has been sitting for months with no updates from the author,
we may choose to close it. This is to avoid cluttering the pull request queue,
which could lead to other pull requests being lost. If your pull request is
closed due to inactivity, you are always welcome to reopen it if/when you have
time to address any issues that were brought up on it.

We do our best to review all pull requests in a timely fashion, but since the
Flare development team has limited time, we may sometimes take a while to review
a pull request. If your pull request has not received a review for a week or
more, feel free to ping relevant individuals or teams on the pull request.

### Reommended Process

The first thing you need to do is
[fork the Flare repository](https://help.github.com/en/articles/fork-a-repo).

Once you have a forked repository, clone and set it up locally:

```bash
# Replace <your-name> with your GitHub user name.
git clone git@github.com:<your-name>/flare.git
cd flare
# Set up a remote pointing to the upstream Flare repository.
git remote add upstream git@github.com:flare-lang/flare.git
```

Whenever you need to update your fork, do something like this:

```bash
git checkout master
# Rebase when pulling to avoid creating merge commits if you have commits that
# have not yet yet been upstreamed.
git pull --rebase upstream master
```

We recommend that you use a feature branch for each individual change, feature,
or fix that you work on:

```bash
# Replace <branch-name> with a name that makes sense for your work.
git checkout -b <branch-name>
```

At this point, you can start making changes and committing them. It is good form
to clean up your commit history to make it easier to review (e.g. if you had to
do small fixups to a commit, squash them into that commit). See
[this article](https://help.github.com/en/articles/about-git-rebase) for more
information.

You may need to rebase your feature branch on top of upstream `master` every now
and then. Use `git pull --rebase upstream master` as shown above. Please always
use rebasing instead of creating merge commits.

Once you are ready to publish your branch, do something like this:

```bash
# Replace <branch-name> with the branch name you picked earlier.
git push origin <branch-name>
```

(You can use the `--force` option if you know what you are doing and you need to
overwrite history in your feature branch that you have already pushed.)

Finally, you can open a pull request against the upstream `master` branch.
