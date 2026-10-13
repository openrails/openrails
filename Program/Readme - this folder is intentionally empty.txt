The contents of this directory have been removed from the Open Rails Subversion 
repository because:

* It serves no purpose when we have automatic builds of every version.
* It prohibits certain styles of development, such as only committing parts of 
  a larger feature.
* It has allowed people to (accidentally) commit binaries that do not match up 
  with the source code.
* It causes grief among developers who constantly get conflicts when updating 
  to the latest code.

The other parts of the Subversion repository - mostly Documentation and Source 
- are untouched. You can continue to use TortoiseSVN to see what the latest 
commits are (although I also have a website for that). This also brings us 
into line with many other open source projects, who only host their source 
code in their repository.

If you wish to stay up-to-date with Open Rails development versions, you have 
two choices:

* Weekly experimental releases from http://openrails.org/experimental.html
* Automatic builds from http://james-ross.co.uk/projects/or/builds

Both of the above options include a built-in updater which will alert you 
whenever a new version of that particular release is available and allow you 
to update to it with just two clicks (one to accept the update and one to 
restart Open Rails).

If you have any unanswered questions, please use this forum thread:
    http://www.elvastower.com/forums/index.php?/topic/24096-subversion-no-longer-hosts-compiled-code/

Thanks.