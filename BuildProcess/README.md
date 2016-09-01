# Amadeus NAnt Build Process - Shared Scripts

## Adding to Your Git Repo

1. Add the NAnt-Build-Process Repo as a remote:
     
    ```
    $ git remote add acg-nant-scripts git@git.wolfgang.com:innovation/nant-build-process
    ```
    
2. Fetch the remote repo (to bring down it's branches locally)
    
    ```
    $ git fetch acg-nant-scripts
    ```
    
3. Add the Amadeus NAnt Scripts master branch as a subtree in your repository

    ```
    $ git subtree add --squash -P BuildProcess/ acg-nant-scripts master
    ```
    
4. Push your changes!
    
    ```
    $ git push
    ```

### *Notes* 
- *Git Subtree comes with Git version 1.7.11 or higher, so you may need to update or install it from the contrib packages*
- *You may chose a different branch/commit than **master** for the NAnt-Build-Process repo, if you require a certain version*