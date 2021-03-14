#!/usr/bin/env python3

# Configuration file for the Sphinx documentation builder.
#
# This file only contains a selection of the most common options. For a full
# list see the documentation:
# https://www.sphinx-doc.org/en/master/usage/configuration.html

# -- Path setup --------------------------------------------------------------

# If extensions (or modules to document with autodoc) are in another directory,
# add these directories to sys.path here. If the directory is relative to the
# documentation root, use os.path.abspath to make it absolute, like shown here.
#
# import os
# import sys
# sys.path.insert(0, os.path.abspath('.'))


# -- Project information -----------------------------------------------------

project = 'Open Rails'
copyright = '2020, Open Rails Team'
author = 'Open Rails Team'
version = ''
revision = ''

# Load current version and revision information (ignoring errors)
try:
    exec(open("./version.py").read())
except:
    pass


# -- General configuration ---------------------------------------------------

# Add any Sphinx extension module names here, as strings. They can be
# extensions coming with Sphinx (named 'sphinx.ext.*') or your custom
# ones.
extensions = [
]

# Add any paths that contain templates here, relative to this directory.
templates_path = ['_templates']

# List of patterns, relative to source directory, that match files and
# directories to ignore when looking for source files.
# This pattern also affects html_static_path and html_extra_path.
exclude_patterns = ['_build', 'Thumbs.db', '.DS_Store']

# These values determine how to format the current date, used as the
# replacement for |today|.
#  - If you set today to a non-empty value, it is used.
#  - Otherwise, the current time is formatted using time.strftime() and the
#    format given in today_fmt.
# The default is now today and a today_fmt of '%B %d, %Y' (or, if translation
# is enabled with language, an equivalent format for the selected locale).
today_fmt = '%d %B %Y'

# NOTE: This is needed because ReadTheDocs uses an old version of Sphinx.
# The master toctree document.
master_doc = 'index'


# -- Options for HTML output -------------------------------------------------

# The theme to use for HTML and HTML Help pages.  See the documentation for
# a list of builtin themes.
#
html_theme = 'sphinx_rtd_theme'

# The “title” for HTML documentation generated with Sphinx’s own templates.
# This is appended to the <title> tag of individual pages, and used in the
# navigation bar as the “topmost” element. It defaults to '<project>
# v<revision> documentation'.
html_title = project + ' ' + version + ' Manual'

# The name of an image file (relative to this directory) to place at the top
# of the sidebar.
html_logo = 'images/or_logo.png'

# Add any paths that contain custom static files (such as style sheets) here,
# relative to this directory. They are copied after the builtin static files,
# so a file named "default.css" will overwrite the builtin "default.css".
html_static_path = ['_static']


# -- Options for LaTex output ------------------------------------------------

# This value determines how to group the document tree into LaTeX source
# files. It must be a list of tuples (startdocname, targetname, title,
# author, theme, toctree_only).
latex_documents = [
    ('index', 'Manual.tex', html_title, author, 'manual')
]

# If given, this must be the name of an image file (relative to the
# configuration directory) that is the logo of the docs. It is placed at the
# top of the title page. Default: None.
latex_logo = 'images/cover_image.png'

latex_elements = {
    # Paper size option of the document class ('a4paper' or 'letterpaper')
    'papersize': 'a4paper',

    # Inclusion of the “fncychap” package (which makes fancy chapter titles),
    # default '\\usepackage[Bjarne]{fncychap}' for English documentation
    # (this option is slightly customized by Sphinx),
    # '\\usepackage[Sonny]{fncychap}' for internationalized docs (because
    # the “Bjarne” style uses numbers spelled out in English). Other
    # “fncychap” styles you can try are “Lenny”, “Glenn”, “Conny”, “Rejne”
    # and “Bjornstrup”. You can also set this to '' to disable fncychap.
    'fncychap': '\\usepackage[Sonny]{fncychap}',

    # Additional preamble content.
    'preamble': '\\usepackage[default]{lato}\\usepackage{inconsolata}\\usepackage{amssymb}',

    # The default is the empty string. Example: 'extraclassoptions':
    # 'openany' will allow chapters (for documents of the 'manual' type) to
    # start on any page.
    'extraclassoptions': 'openany',
}
