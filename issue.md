
# 问题改进

1. TokenManager中引用了AuthorizationManager. Manager之间不应互相引用。需要复用的应该封装成 service，或者移动到CommonMod模块.

