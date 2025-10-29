
# 问题改进

1. TokenManager中引用了AuthorizationManager. Manager之间不应互相引用。需要复用的应该封装成 service，或者移动到CommonMod模块.
2. 测试项目没有使用CPM
3. 时常会缺少命名空间
4. 测试项目引用的包版本错误